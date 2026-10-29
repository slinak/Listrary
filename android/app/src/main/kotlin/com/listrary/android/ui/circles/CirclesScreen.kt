package com.listrary.android.ui.circles

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.PullToRefreshBox
import androidx.compose.material3.Scaffold
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
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.listrary.android.R
import com.listrary.android.ui.common.EmptyState
import com.listrary.android.ui.common.ErrorState
import com.listrary.android.ui.common.LoadingState

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CirclesScreen(
    onOpenCircle: (Int) -> Unit,
    viewModel: CirclesViewModel = hiltViewModel(),
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbar = remember { SnackbarHostState() }
    val context = LocalContext.current
    var showCreate by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        viewModel.messageFlow.collect { snackbar.showSnackbar(context.getString(it)) }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text(stringResource(R.string.circles_title)) }) },
        snackbarHost = { SnackbarHost(snackbar) },
        floatingActionButton = {
            FloatingActionButton(onClick = { showCreate = true }) {
                Icon(Icons.Filled.Add, stringResource(R.string.new_circle))
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = state.isRefreshing,
            onRefresh = viewModel::refresh,
            modifier = Modifier.padding(padding).fillMaxSize(),
        ) {
            val empty = state.circles.isEmpty() && state.invitations.isEmpty()
            when {
                empty && !state.hasLoaded && state.errorRes == null -> LoadingState()
                empty && state.errorRes != null -> ErrorState(state.errorRes!!, onRetry = viewModel::refresh)
                empty -> EmptyState(R.string.empty_circles)
                else -> LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 88.dp)) {
                    if (state.invitations.isNotEmpty()) {
                        item(key = "h-invites") {
                            Text(
                                stringResource(R.string.pending_invitations),
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.padding(start = 16.dp, top = 16.dp, bottom = 4.dp),
                            )
                        }
                        items(state.invitations, key = { "i-${it.circleInvitationId}" }) { invitation ->
                            ListItem(
                                headlineContent = {
                                    Text(
                                        stringResource(
                                            R.string.circle_invited_by,
                                            invitation.circleName ?: "#${invitation.circleId}",
                                        ),
                                    )
                                },
                                supportingContent = {
                                    Row {
                                        TextButton(onClick = { viewModel.accept(invitation) }) {
                                            Text(stringResource(R.string.action_accept))
                                        }
                                        TextButton(onClick = { viewModel.reject(invitation) }) {
                                            Text(stringResource(R.string.action_reject))
                                        }
                                    }
                                },
                            )
                            HorizontalDivider()
                        }
                    }
                    if (state.circles.isNotEmpty()) {
                        item(key = "h-circles") {
                            Text(
                                stringResource(R.string.circles_title),
                                style = MaterialTheme.typography.titleSmall,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.padding(start = 16.dp, top = 16.dp, bottom = 4.dp),
                            )
                        }
                        items(state.circles, key = { "c-${it.circleId}" }) { circle ->
                            ListItem(
                                modifier = Modifier.clickable { onOpenCircle(circle.circleId) },
                                leadingContent = { Icon(Icons.Filled.Groups, null) },
                                headlineContent = { Text(circle.name) },
                            )
                            HorizontalDivider()
                        }
                    }
                }
            }
        }
    }

    if (showCreate) {
        var name by remember { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { showCreate = false },
            title = { Text(stringResource(R.string.new_circle)) },
            text = {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text(stringResource(R.string.circle_name)) },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
            },
            confirmButton = {
                TextButton(enabled = name.isNotBlank(), onClick = {
                    showCreate = false
                    viewModel.createCircle(name)
                }) { Text(stringResource(R.string.action_create)) }
            },
            dismissButton = {
                TextButton(onClick = { showCreate = false }) { Text(stringResource(R.string.action_cancel)) }
            },
        )
    }
}