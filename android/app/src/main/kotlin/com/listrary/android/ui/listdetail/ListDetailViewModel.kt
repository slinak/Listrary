package com.listrary.android.ui.listdetail

import androidx.annotation.StringRes
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.listrary.android.R
import com.listrary.android.data.local.entities.ListEntity
import com.listrary.android.data.local.entities.ListItemEntity
import com.listrary.android.data.local.entities.ListShareEntity
import com.listrary.android.data.local.entities.ListType
import com.listrary.android.data.local.entities.SharePermission
import com.listrary.android.data.repository.ListItemsRepository
import com.listrary.android.data.repository.ListsRepository
import com.listrary.android.ui.common.toMessageRes
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import retrofit2.HttpException
import javax.inject.Inject

sealed interface SectionKind {
    /** Incomplete items sharing a store location (name == null means no location). */
    data class Location(val name: String?) : SectionKind
    data object Pending : SectionKind
    data object Completed : SectionKind
}

data class ItemSection(val kind: SectionKind, val items: List<ListItemEntity>)

data class ItemDraft(val name: String = "", val location: String = "")

data class ListDetailUiState(
    val list: ListEntity? = null,
    val sections: List<ItemSection> = emptyList(),
    val shares: List<ListShareEntity> = emptyList(),
    val canEdit: Boolean = false,
    val isRefreshing: Boolean = false,
    val hasLoaded: Boolean = false,
    @StringRes val errorRes: Int? = null,
    val generatedLink: String? = null,
    val isSharing: Boolean = false,
) {
    val isShopping get() = list?.let { ListType.fromApi(it.listType) == ListType.Shopping } == true
    val itemCount get() = sections.sumOf { it.items.size }
}

@OptIn(FlowPreview::class, ExperimentalCoroutinesApi::class)
@HiltViewModel
class ListDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val listsRepository: ListsRepository,
    private val itemsRepository: ListItemsRepository,
) : ViewModel() {

    val listId: Int = checkNotNull(savedStateHandle["listId"])

    /** Present when opened through a share link; sent with every request for this list. */
    private val shareToken: String? = savedStateHandle.get<String>("shareToken")?.takeIf { it.isNotBlank() }

    val openedViaShareLink: Boolean get() = shareToken != null

    private data class Transient(
        val isRefreshing: Boolean = false,
        val hasLoaded: Boolean = false,
        val errorRes: Int? = null,
        val generatedLink: String? = null,
        val isSharing: Boolean = false,
    )

    private val transient = MutableStateFlow(Transient())
    private val _draft = MutableStateFlow(ItemDraft())
    val draft: StateFlow<ItemDraft> = _draft

    private val messages = Channel<Int>(Channel.BUFFERED)
    val messageFlow = messages.receiveAsFlow()

    val uiState: StateFlow<ListDetailUiState> = combine(
        listsRepository.observeList(listId),
        itemsRepository.observeItems(listId),
        listsRepository.observeShares(listId),
        transient,
    ) { list, items, shares, t ->
        ListDetailUiState(
            list = list,
            sections = buildSections(list, items),
            shares = shares,
            canEdit = list?.canEdit ?: false,
            isRefreshing = t.isRefreshing,
            hasLoaded = t.hasLoaded,
            errorRes = t.errorRes,
            generatedLink = t.generatedLink,
            isSharing = t.isSharing,
        )
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), ListDetailUiState())

    val nameSuggestions: StateFlow<List<String>> = _draft
        .map { it.name.trim() }
        .distinctUntilChanged()
        .debounce(300)
        .flatMapLatest { query ->
            if (query.length < 2) flowOf(emptyList())
            else flow { emit(itemsRepository.suggestNames(listId, query, shareToken).getOrDefault(emptyList())) }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    val locationSuggestions: StateFlow<List<String>> = _draft
        .map { it.name.trim() to it.location.trim() }
        .distinctUntilChanged()
        .debounce(300)
        .flatMapLatest { (name, location) ->
            if (name.isEmpty()) flowOf(emptyList())
            else flow {
                emit(itemsRepository.suggestLocations(listId, name, location, shareToken).getOrDefault(emptyList()))
            }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            transient.update { it.copy(isRefreshing = true, errorRes = null) }
            val result = listsRepository.refreshDetail(listId, shareToken)
            transient.update {
                it.copy(isRefreshing = false, hasLoaded = true, errorRes = result.exceptionOrNull()?.toMessageRes())
            }
        }
    }

    fun onNameChange(value: String) = _draft.update { it.copy(name = value) }
    fun onLocationChange(value: String) = _draft.update { it.copy(location = value) }

    fun addItem() {
        val current = _draft.value
        if (current.name.isBlank()) return
        viewModelScope.launch {
            itemsRepository.addItem(listId, current.name, null, current.location, shareToken)
                .onSuccess { warning ->
                    // Keep the location so consecutive items in the same aisle are quick to add.
                    _draft.value = ItemDraft(location = current.location)
                    if (!warning.isNullOrBlank()) messages.send(R.string.store_locations_similar_warning)
                }
                .onFailure { messages.send(itemErrorRes(it, current.location)) }
        }
    }

    fun updateItem(item: ListItemEntity, name: String, details: String?, location: String?) {
        viewModelScope.launch {
            itemsRepository.updateItem(listId, item.listItemId, name, details, location, item.isCompleted, shareToken)
                .onSuccess { warning ->
                    if (!warning.isNullOrBlank()) messages.send(R.string.store_locations_similar_warning)
                }
                .onFailure { messages.send(itemErrorRes(it, location)) }
        }
    }

    fun toggle(item: ListItemEntity) {
        viewModelScope.launch {
            itemsRepository.toggleItem(listId, item.listItemId, shareToken)
                .onFailure { messages.send(it.toMessageRes()) }
        }
    }

    fun deleteItem(item: ListItemEntity) {
        viewModelScope.launch {
            itemsRepository.deleteItem(listId, item.listItemId, shareToken)
                .onFailure { messages.send(it.toMessageRes()) }
        }
    }

    fun markAllIncomplete() {
        viewModelScope.launch {
            itemsRepository.markAllIncomplete(listId, shareToken)
                .onFailure { messages.send(it.toMessageRes()) }
        }
    }

    fun createShare(permission: SharePermission) {
        viewModelScope.launch {
            transient.update { it.copy(isSharing = true) }
            listsRepository.createShare(listId, permission)
                .onSuccess { share -> transient.update { it.copy(generatedLink = share.shareLink) } }
                .onFailure { messages.send(it.toMessageRes()) }
            transient.update { it.copy(isSharing = false) }
        }
    }

    fun clearGeneratedLink() = transient.update { it.copy(generatedLink = null) }

    /** A 400 on a strict list with a location is the backend's StoreLocations.StrictError. */
    private fun itemErrorRes(error: Throwable, location: String?): Int {
        val strict = uiState.value.list?.strictStoreLocations == true
        return if (strict && !location.isNullOrBlank() && error is HttpException && error.code() in 400..422) {
            R.string.store_locations_strict_error
        } else {
            error.toMessageRes()
        }
    }

    /** Incomplete items first (grouped by store location for Shopping lists), completed last. */
    private fun buildSections(list: ListEntity?, items: List<ListItemEntity>): List<ItemSection> {
        val incomplete = items.filter { !it.isCompleted }.sortedBy { it.sortOrder }
        val completed = items.filter { it.isCompleted }.sortedBy { it.sortOrder }
        val sections = mutableListOf<ItemSection>()
        if (list != null && ListType.fromApi(list.listType) == ListType.Shopping) {
            val byLocation = incomplete.groupBy { it.storeLocation?.trim()?.takeIf { l -> l.isNotEmpty() } }
            byLocation.keys.filterNotNull().sortedBy { it.lowercase() }.forEach { loc ->
                sections += ItemSection(SectionKind.Location(loc), byLocation.getValue(loc))
            }
            byLocation[null]?.let { sections += ItemSection(SectionKind.Location(null), it) }
        } else if (incomplete.isNotEmpty()) {
            sections += ItemSection(SectionKind.Pending, incomplete)
        }
        if (completed.isNotEmpty()) sections += ItemSection(SectionKind.Completed, completed)
        return sections
    }
}