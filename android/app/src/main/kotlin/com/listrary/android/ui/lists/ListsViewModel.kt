package com.listrary.android.ui.lists

import androidx.annotation.StringRes
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.ListEntity
import com.listrary.android.data.local.entities.ListType
import com.listrary.android.data.repository.CirclesRepository
import com.listrary.android.data.repository.ListsRepository
import com.listrary.android.ui.common.toMessageRes
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class CircleListGroup(val circleId: Int, val circleName: String, val lists: List<ListEntity>)

data class ListsUiState(
    val circleGroups: List<CircleListGroup> = emptyList(),
    val myLists: List<ListEntity> = emptyList(),
    val circles: List<CircleEntity> = emptyList(),
    val isRefreshing: Boolean = false,
    val hasLoaded: Boolean = false,
    @StringRes val errorRes: Int? = null,
) {
    val isEmpty get() = circleGroups.isEmpty() && myLists.isEmpty()
}

@HiltViewModel
class ListsViewModel @Inject constructor(
    private val listsRepository: ListsRepository,
    private val circlesRepository: CirclesRepository,
) : ViewModel() {

    private data class Transient(val isRefreshing: Boolean = false, val hasLoaded: Boolean = false, val errorRes: Int? = null)

    private val transient = MutableStateFlow(Transient())
    private val messages = Channel<Int>(Channel.BUFFERED)
    val messageFlow = messages.receiveAsFlow()

    val uiState: StateFlow<ListsUiState> = combine(
        listsRepository.observeSummaries(),
        circlesRepository.observeCircles(),
        transient,
    ) { lists, circles, t ->
        val grouped = lists.filter { it.circleId != null }
            .groupBy { it.circleId!! }
            .map { (id, items) ->
                CircleListGroup(
                    circleId = id,
                    circleName = items.firstNotNullOfOrNull { it.circleName }
                        ?: circles.firstOrNull { it.circleId == id }?.name.orEmpty(),
                    lists = items,
                )
            }
            .sortedBy { it.circleName.lowercase() }
        ListsUiState(
            circleGroups = grouped,
            myLists = lists.filter { it.circleId == null },
            circles = circles,
            isRefreshing = t.isRefreshing,
            hasLoaded = t.hasLoaded,
            errorRes = t.errorRes,
        )
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), ListsUiState())

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            transient.update { it.copy(isRefreshing = true, errorRes = null) }
            val lists = listsRepository.refresh()
            circlesRepository.refresh()
            transient.update {
                it.copy(
                    isRefreshing = false,
                    hasLoaded = true,
                    errorRes = lists.exceptionOrNull()?.toMessageRes(),
                )
            }
        }
    }

    fun createList(name: String, type: ListType, circleId: Int?, strict: Boolean) {
        viewModelScope.launch {
            listsRepository.createList(name, type, circleId, strict)
                .onFailure { messages.send(it.toMessageRes()) }
        }
    }

    fun updateList(listId: Int, name: String, type: ListType, circleId: Int?, strict: Boolean) {
        viewModelScope.launch {
            listsRepository.updateList(listId, name, type, circleId, strict)
                .onFailure { messages.send(it.toMessageRes()) }
        }
    }

    fun deleteList(listId: Int) {
        viewModelScope.launch {
            listsRepository.deleteList(listId).onFailure { messages.send(it.toMessageRes()) }
        }
    }
}