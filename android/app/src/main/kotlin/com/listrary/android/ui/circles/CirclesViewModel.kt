package com.listrary.android.ui.circles

import androidx.annotation.StringRes
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.listrary.android.R
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.CircleInvitationEntity
import com.listrary.android.data.repository.CirclesRepository
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

data class CirclesUiState(
    val circles: List<CircleEntity> = emptyList(),
    val invitations: List<CircleInvitationEntity> = emptyList(),
    val isRefreshing: Boolean = false,
    val hasLoaded: Boolean = false,
    @StringRes val errorRes: Int? = null,
)

@HiltViewModel
class CirclesViewModel @Inject constructor(
    private val repository: CirclesRepository,
) : ViewModel() {

    private data class Transient(val isRefreshing: Boolean = false, val hasLoaded: Boolean = false, val errorRes: Int? = null)

    private val transient = MutableStateFlow(Transient())
    private val messages = Channel<Int>(Channel.BUFFERED)
    val messageFlow = messages.receiveAsFlow()

    val uiState: StateFlow<CirclesUiState> = combine(
        repository.observeCircles(),
        repository.observeIncomingInvitations(),
        transient,
    ) { circles, invitations, t ->
        CirclesUiState(circles, invitations, t.isRefreshing, t.hasLoaded, t.errorRes)
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), CirclesUiState())

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            transient.update { it.copy(isRefreshing = true, errorRes = null) }
            val result = repository.refresh()
            transient.update {
                it.copy(isRefreshing = false, hasLoaded = true, errorRes = result.exceptionOrNull()?.toMessageRes())
            }
        }
    }

    fun createCircle(name: String) = run(R.string.circles_status_created) { repository.createCircle(name) }
    fun accept(invitation: CircleInvitationEntity) =
        run(R.string.circles_status_accepted) { repository.acceptInvitation(invitation.circleInvitationId) }
    fun reject(invitation: CircleInvitationEntity) =
        run(R.string.circles_status_rejected) { repository.rejectInvitation(invitation.circleInvitationId) }

    private fun run(@StringRes success: Int, block: suspend () -> Result<Unit>) {
        viewModelScope.launch {
            block().fold(
                onSuccess = { messages.send(success) },
                onFailure = { messages.send(it.toMessageRes()) },
            )
        }
    }
}