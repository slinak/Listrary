package com.listrary.android.ui.lists

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
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
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.ListEntity
import com.listrary.android.data.local.entities.ListType
import com.listrary.android.ui.common.DropdownField
import com.listrary.android.ui.common.EmptyState
import com.listrary.android.ui.common.ErrorState
import com.listrary.android.ui.common.LoadingState
import com.listrary.android.ui.common.SwipeToDeleteBox
import com.listrary.android.ui.common.icon
import com.listrary.android.ui.common.labelRes

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ListsScreen(
    onOpenList: (Int) -> Unit,
    viewModel: ListsViewModel = hiltViewModel(),
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbar = remember { SnackbarHostState() }
    val context = LocalContext.current
    var formTarget by remember { mutableStateOf<ListEntity?>(null) }
    var showCreate by remember { mutableStateOf(false) }
    var deleteTarget by remember { mutableStateOf<ListEntity?>(null) }

    LaunchedEffect(Unit) {
        viewModel.messageFlow.collect { snackbar.showSnackbar(context.getString(it)) }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text(stringResource(R.string.lists_title)) }) },
        snackbarHost = { SnackbarHost(snackbar) },
        floatingActionButton = {
            FloatingActionButton(onClick = { showCreate = true }) {
                Icon(Icons.Filled.Add, contentDescription = stringResource(R.string.new_list))
            }
        },
    ) { padding ->
        PullToRefreshBox(
            isRefreshing = state.isRefreshing,
            onRefresh = viewModel::refresh,
            modifier = Modifier.padding(padding).fillMaxSize(),
        ) {
            when {
                !state.hasLoaded && state.isEmpty && state.errorRes == null -> LoadingState()
                state.isEmpty && state.errorRes != null ->
                    ErrorState(state.errorRes!!, onRetry = viewModel::refresh)
                state.isEmpty -> EmptyState(R.string.empty_lists)
                else -> LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 88.dp)) {
                    state.circleGroups.forEach { group ->
                        item(key = "h-${group.circleId}") { SectionHeader(group.circleName) }
                        items(group.lists, key = { "l-${it.listId}" }) { list ->
                            ListRow(list, onOpenList, onEdit = { formTarget = it }, onDelete = { deleteTarget = it })
                        }
                    }
                    if (state.myLists.isNotEmpty()) {
                        item(key = "h-mine") { SectionHeader(stringResource(R.string.my_lists)) }
                        items(state.myLists, key = { "l-${it.listId}" }) { list ->
                            ListRow(list, onOpenList, onEdit = { formTarget = it }, onDelete = { deleteTarget = it })
                        }
                    }
                }
            }
        }
    }

    if (showCreate) {
        ListFormDialog(
            initial = null,
            circles = state.circles,
            onDismiss = { showCreate = false },
            onConfirm = { name, type, circleId, strict ->
                showCreate = false
                viewModel.createList(name, type, circleId, strict)
            },
        )
    }
    formTarget?.let { target ->
        ListFormDialog(
            initial = target,
            circles = state.circles,
            onDismiss = { formTarget = null },
            onConfirm = { name, type, circleId, strict ->
                formTarget = null
                viewModel.updateList(target.listId, name, type, circleId, strict)
            },
        )
    }
    deleteTarget?.let { target ->
        AlertDialog(
            onDismissRequest = { deleteTarget = null },
            title = { Text(stringResource(R.string.delete_list)) },
            text = { Text(stringResource(R.string.delete_list_confirm, target.listName)) },
            confirmButton = {
                TextButton(onClick = {
                    deleteTarget = null
                    viewModel.deleteList(target.listId)
                }) { Text(stringResource(R.string.action_delete)) }
            },
            dismissButton = {
                TextButton(onClick = { deleteTarget = null }) { Text(stringResource(R.string.action_cancel)) }
            },
        )
    }
}

@Composable
private fun SectionHeader(title: String) {
    Text(
        title,
        style = MaterialTheme.typography.titleSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.fillMaxWidth().padding(start = 16.dp, end = 16.dp, top = 16.dp, bottom = 4.dp),
    )
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun ListRow(
    list: ListEntity,
    onOpen: (Int) -> Unit,
    onEdit: (ListEntity) -> Unit,
    onDelete: (ListEntity) -> Unit,
) {
    var menuOpen by remember { mutableStateOf(false) }
    val type = ListType.fromApi(list.listType)
    SwipeToDeleteBox(onDelete = { onDelete(list) }) {
        Column(Modifier.background(MaterialTheme.colorScheme.surface)) {
            ListItem(
                modifier = Modifier.combinedClickable(
                    onClick = { onOpen(list.listId) },
                    onLongClick = { menuOpen = true },
                ),
                leadingContent = { Icon(type.icon, contentDescription = null) },
                headlineContent = { Text(list.listName) },
                supportingContent = {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text(stringResource(type.labelRes))
                        Text(" · " + stringResource(R.string.items_count, list.itemCount))
                    }
                },
            )
            DropdownMenu(expanded = menuOpen, onDismissRequest = { menuOpen = false }) {
                DropdownMenuItem(
                    text = { Text(stringResource(R.string.action_edit)) },
                    onClick = { menuOpen = false; onEdit(list) },
                )
                DropdownMenuItem(
                    text = { Text(stringResource(R.string.action_delete)) },
                    onClick = { menuOpen = false; onDelete(list) },
                )
            }
            HorizontalDivider()
        }
    }
}

@Composable
fun ListFormDialog(
    initial: ListEntity?,
    circles: List<CircleEntity>,
    onDismiss: () -> Unit,
    onConfirm: (name: String, type: ListType, circleId: Int?, strict: Boolean) -> Unit,
) {
    var name by remember { mutableStateOf(initial?.listName.orEmpty()) }
    var type by remember { mutableStateOf(ListType.fromApi(initial?.listType)) }
    var circleId by remember { mutableStateOf(initial?.circleId) }
    var strict by remember { mutableStateOf(initial?.strictStoreLocations ?: false) }

    val typeOptions = ListType.entries.map { it to stringResource(it.labelRes) }
    val noCircle = stringResource(R.string.list_no_circle)
    val circleOptions = listOf<Pair<Int?, String>>(null to noCircle) + circles.map { it.circleId to it.name }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(stringResource(if (initial == null) R.string.new_list else R.string.edit_list)) },
        text = {
            Column(verticalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text(stringResource(R.string.list_name)) },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                DropdownField(
                    label = stringResource(R.string.list_type),
                    selectedLabel = stringResource(type.labelRes),
                    options = typeOptions,
                    onSelected = { type = it },
                )
                DropdownField(
                    label = stringResource(R.string.list_circle_optional),
                    selectedLabel = circleOptions.firstOrNull { it.first == circleId }?.second ?: noCircle,
                    options = circleOptions,
                    onSelected = { circleId = it },
                )
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Column(Modifier.weight(1f)) {
                        Text(stringResource(R.string.list_strict_store_locations))
                        Text(
                            stringResource(R.string.list_strict_store_locations_help),
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                    Switch(checked = strict, onCheckedChange = { strict = it })
                }
            }
        },
        confirmButton = {
            TextButton(
                enabled = name.isNotBlank(),
                onClick = { onConfirm(name, type, circleId, strict) },
            ) { Text(stringResource(if (initial == null) R.string.action_create else R.string.action_save)) }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text(stringResource(R.string.action_cancel)) } },
    )
}