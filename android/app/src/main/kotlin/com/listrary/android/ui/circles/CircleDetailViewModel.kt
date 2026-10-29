package com.listrary.android.ui.circles

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.listrary.android.R
import com.listrary.android.auth.AuthState
import com.listrary.android.auth.MsalAuthManager
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.CircleInvitationEntity
import com.listrary.android.data.local.entities.CircleMemberEntity
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

data class CircleDetailUiState(
    val circle: CircleEntity? = null,
    val members: List<CircleMemberEntity> = emptyList(),
    val invitations: List<CircleInvitationEntity> = emptyList(),
    val currentUserId: String = "",
    val isRefreshing: Boolean = false,
    val errorRes: Int? = null,
) {
    val isOwner get() = circle != null && circle.ownerId == currentUserId
}

sealed interface CircleDetailEvent {
    data class Message(val resId: Int) : CircleDetailEvent
    data object Close : CircleDetailEvent
}

@HiltViewModel
class CircleDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val repository: CirclesRepository,
    authManager: MsalAuthManager,
) : ViewModel() {

    private val circleId: Int = checkNotNull(savedStateHandle["circleId"])

    private data class Transient(val isRefreshing: Boolean = false, val errorRes: Int? = null)

    private val transient = MutableStateFlow(Transient())
    private val events = Channel<CircleDetailEvent>(Channel.BUFFERED)
    val eventFlow = events.receiveAsFlow()

    val uiState: StateFlow<CircleDetailUiState> = combine(
        repository.observeCircle(circleId),
        repository.observeMembers(circleId),
        repository.observeInvitations(circleId),
        authManager.state,
        transient,
    ) { circle, members, invitations, auth, t ->
        CircleDetailUiState(
            circle = circle,
            members = members,
            invitations = invitations,
            currentUserId = (auth as? AuthState.SignedIn)?.userId.orEmpty(),
            isRefreshing = t.isRefreshing,
            errorRes = t.errorRes,
        )
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), CircleDetailUiState())

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            transient.update { it.copy(isRefreshing = true, errorRes = null) }
            val result = repository.refreshCircle(circleId)
            transient.update { it.copy(isRefreshing = false, errorRes = result.exceptionOrNull()?.toMessageRes()) }
        }
    }

    /** Input containing '@' is treated as an email address, otherwise as a user id. */
    fun invite(input: String) {
        val value = input.trim()
        if (value.isEmpty()) return
        val isEmail = '@' in value
        act(R.string.circles_status_invited) {
            repository.invite(circleId, if (isEmail) value else null, if (isEmail) null else value)
        }
    }

    fun removeMember(member: CircleMemberEntity) =
        act(R.string.circles_status_member_removed) { repository.removeMember(circleId, member.userId) }

    fun cancelInvitation(invitation: CircleInvitationEntity) =
        act(R.string.circles_status_cancelled) { repository.cancelInvitation(invitation.circleInvitationId) }

    fun leave() = act(R.string.circles_status_left, close = true) { repository.leave(circleId) }

    fun deleteCircle() = act(R.string.circles_status_deleted, close = true) { repository.deleteCircle(circleId) }

    private fun act(successRes: Int, close: Boolean = false, block: suspend () -> Result<Unit>) {
        viewModelScope.launch {
            block().fold(
                onSuccess = {
                    events.send(CircleDetailEvent.Message(successRes))
                    if (close) events.send(CircleDetailEvent.Close)
                },
                onFailure = { events.send(CircleDetailEvent.Message(it.toMessageRes())) },
            )
        }
    }
}