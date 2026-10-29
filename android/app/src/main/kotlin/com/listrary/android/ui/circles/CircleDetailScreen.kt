package com.listrary.android.ui.circles

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.PersonRemove
import androidx.compose.material.icons.filled.Send
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.listrary.android.R
import com.listrary.android.ui.common.ErrorState
import com.listrary.android.ui.common.LoadingState

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CircleDetailScreen(
    onBack: () -> Unit,
    viewModel: CircleDetailViewModel = hiltViewModel(),
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbar = remember { SnackbarHostState() }
    val context = LocalContext.current
    var inviteInput by remember { mutableStateOf("") }
    var confirmLeave by remember { mutableStateOf(false) }
    var confirmDelete by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.eventFlow.collect { event ->
            when (event) {
                is CircleDetailEvent.Message -> snackbar.showSnackbar(context.getString(event.resId))
                CircleDetailEvent.Close -> onBack()
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.circle?.name.orEmpty()) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, stringResource(R.string.action_back))
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbar) },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = state.isRefreshing,
            onRefresh = viewModel::refresh,
            modifier = Modifier.padding(padding).fillMaxSize(),
        ) {
            val circle = state.circle
            when {
                circle == null && state.errorRes != null -> ErrorState(state.errorRes!!, onRetry = viewModel::refresh)
                circle == null -> LoadingState()
                else -> LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 24.dp)) {
                    item(key = "h-members") { Header(stringResource(R.string.circle_members)) }
                    items(state.members, key = { "m-${it.circleMemberId}" }) { member ->
                        val tags = buildList {
                            if (member.userId == circle.ownerId) add(stringResource(R.string.circle_owner))
                            if (member.userId == state.currentUserId) add(stringResource(R.string.circle_you))
                        }.joinToString(" · ")
                        ListItem(
                            headlineContent = { Text(member.userId) },
                            supportingContent = if (tags.isNotEmpty()) ({ Text(tags) }) else null,
                            trailingContent = if (state.isOwner && member.userId != circle.ownerId) {
                                {
                                    IconButton(onClick = { viewModel.removeMember(member) }) {
                                        Icon(Icons.Filled.PersonRemove, stringResource(R.string.circle_remove_member))
                                    }
                                }
                            } else null,
                        )
                        HorizontalDivider()
                    }

                    item(key = "h-invitations") { Header(stringResource(R.string.circle_invitations)) }
                    items(state.invitations, key = { "i-${it.circleInvitationId}" }) { invitation ->
                        val canCancel = !invitation.accepted &&
                            (state.isOwner || invitation.inviterId == state.currentUserId)
                        ListItem(
                            headlineContent = { Text(invitation.recipientEmail ?: invitation.recipientId.orEmpty()) },
                            supportingContent = {
                                Text(
                                    stringResource(
                                        if (invitation.accepted) R.string.circle_invitation_accepted
                                        else R.string.circle_invitation_pending,
                                    ),
                                )
                            },
                            trailingContent = if (canCancel) {
                                {
                                    IconButton(onClick = { viewModel.cancelInvitation(invitation) }) {
                                        Icon(Icons.Filled.Close, stringResource(R.string.circle_cancel_invitation))
                                    }
                                }
                            } else null,
                        )
                        HorizontalDivider()
                    }

                    item(key = "invite") {
                        Header(stringResource(R.string.circle_invite_title))
                        Row(
                            Modifier.padding(horizontal = 16.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            OutlinedTextField(
                                value = inviteInput,
                                onValueChange = { inviteInput = it },
                                label = { Text(stringResource(R.string.circle_invite_hint)) },
                                singleLine = true,
                                modifier = Modifier.weight(1f),
                            )
                            IconButton(
                                enabled = inviteInput.isNotBlank(),
                                onClick = {
                                    viewModel.invite(inviteInput)
                                    inviteInput = ""
                                },
                            ) { Icon(Icons.Filled.Send, stringResource(R.string.circle_invite_send)) }
                        }
                    }

                    item(key = "danger") {
                        if (state.isOwner) {
                            Button(
                                onClick = { confirmDelete = true },
                                modifier = Modifier.fillMaxWidth().padding(16.dp),
                            ) { Text(stringResource(R.string.circle_delete)) }
                        } else {
                            OutlinedButton(
                                onClick = { confirmLeave = true },
                                modifier = Modifier.fillMaxWidth().padding(16.dp),
                            ) { Text(stringResource(R.string.circle_leave)) }
                        }
                    }
                }
            }
        }
    }

    val circleName = state.circle?.name.orEmpty()
    if (confirmLeave) {
        ConfirmDialog(
            text = stringResource(R.string.circle_leave_confirm, circleName),
            confirmLabel = stringResource(R.string.circle_leave),
            onConfirm = { confirmLeave = false; viewModel.leave() },
            onDismiss = { confirmLeave = false },
        )
    }
    if (confirmDelete) {
        ConfirmDialog(
            text = stringResource(R.string.circle_delete_confirm, circleName),
            confirmLabel = stringResource(R.string.action_delete),
            onConfirm = { confirmDelete = false; viewModel.deleteCircle() },
            onDismiss = { confirmDelete = false },
        )
    }
}

@Composable
private fun Header(title: String) {
    Text(
        title,
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(start = 16.dp, top = 16.dp, bottom = 4.dp),
    )
}

@Composable
private fun ConfirmDialog(text: String, confirmLabel: String, onConfirm: () -> Unit, onDismiss: () -> Unit) {
    AlertDialog(
        onDismissRequest = onDismiss,
        text = { Text(text) },
        confirmButton = { TextButton(onClick = onConfirm) { Text(confirmLabel) } },
        dismissButton = { TextButton(onClick = onDismiss) { Text(stringResource(R.string.action_cancel)) } },
    )
}