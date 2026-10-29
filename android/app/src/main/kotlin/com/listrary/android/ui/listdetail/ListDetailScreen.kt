package com.listrary.android.ui.listdetail

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Share
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.PullToRefreshBox
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.SuggestionChip
import androidx.compose.material3.Surface
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
import com.listrary.android.data.local.entities.ListItemEntity
import com.listrary.android.ui.common.ErrorState
import com.listrary.android.ui.common.ListItemRow
import com.listrary.android.ui.common.LoadingState
import com.listrary.android.ui.common.SwipeToDeleteBox
import com.listrary.android.ui.share.ShareSheet

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ListDetailScreen(
    onBack: () -> Unit,
    viewModel: ListDetailViewModel = hiltViewModel(),
) {
    val state by viewModel.uiState.collectAsStateWithLifecycle()
    val draft by viewModel.draft.collectAsStateWithLifecycle()
    val nameSuggestions by viewModel.nameSuggestions.collectAsStateWithLifecycle()
    val locationSuggestions by viewModel.locationSuggestions.collectAsStateWithLifecycle()
    val snackbar = remember { SnackbarHostState() }
    val context = LocalContext.current
    var menuOpen by remember { mutableStateOf(false) }
    var showShare by remember { mutableStateOf(false) }
    var editTarget by remember { mutableStateOf<ListItemEntity?>(null) }

    LaunchedEffect(Unit) {
        viewModel.messageFlow.collect { snackbar.showSnackbar(context.getString(it)) }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.list?.listName.orEmpty()) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, stringResource(R.string.action_back))
                    }
                },
                actions = {
                    if (state.canEdit && !viewModel.openedViaShareLink) {
                        IconButton(onClick = { showShare = true }) {
                            Icon(Icons.Filled.Share, stringResource(R.string.share))
                        }
                    }
                    if (state.canEdit) {
                        IconButton(onClick = { menuOpen = true }) {
                            Icon(Icons.Filled.MoreVert, null)
                        }
                        DropdownMenu(expanded = menuOpen, onDismissRequest = { menuOpen = false }) {
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.mark_all_incomplete)) },
                                onClick = { menuOpen = false; viewModel.markAllIncomplete() },
                            )
                        }
                    }
                },
            )
        },
        snackbarHost = { SnackbarHost(snackbar) },
        bottomBar = {
            if (state.canEdit) {
                AddItemBar(
                    draft = draft,
                    showLocation = state.isShopping,
                    nameSuggestions = nameSuggestions,
                    locationSuggestions = locationSuggestions,
                    onNameChange = viewModel::onNameChange,
                    onLocationChange = viewModel::onLocationChange,
                    onAdd = viewModel::addItem,
                )
            }
        },
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            if (state.list != null && !state.canEdit) {
                Surface(color = MaterialTheme.colorScheme.secondaryContainer, modifier = Modifier.fillMaxWidth()) {
                    Text(
                        stringResource(R.string.read_only_banner),
                        style = MaterialTheme.typography.bodyMedium,
                        modifier = Modifier.padding(12.dp),
                    )
                }
            }
            PullToRefreshBox(
                isRefreshing = state.isRefreshing,
                onRefresh = viewModel::refresh,
                modifier = Modifier.fillMaxSize(),
            ) {
                when {
                    state.list == null && state.errorRes != null ->
                        ErrorState(state.errorRes!!, onRetry = viewModel::refresh)
                    state.list == null -> LoadingState()
                    state.itemCount == 0 && state.hasLoaded -> Column(
                        Modifier.fillMaxSize().padding(24.dp),
                        verticalArrangement = Arrangement.Center,
                        horizontalAlignment = Alignment.CenterHorizontally,
                    ) { Text(stringResource(R.string.empty_items)) }
                    else -> LazyColumn(Modifier.fillMaxSize(), contentPadding = PaddingValues(bottom = 16.dp)) {
                        state.sections.forEach { section ->
                            val header = sectionTitle(section.kind)
                            if (header != null) {
                                item(key = "h-${section.kind}") {
                                    Text(
                                        header,
                                        style = MaterialTheme.typography.titleSmall,
                                        color = MaterialTheme.colorScheme.primary,
                                        modifier = Modifier.padding(start = 16.dp, top = 16.dp, bottom = 4.dp),
                                    )
                                }
                            }
                            items(section.items, key = { it.listItemId }) { item ->
                                SwipeToDeleteBox(onDelete = { viewModel.deleteItem(item) }, enabled = state.canEdit) {
                                    Surface {
                                        Column {
                                            ListItemRow(
                                                item = item,
                                                canEdit = state.canEdit,
                                                onToggle = { viewModel.toggle(item) },
                                                onEdit = { editTarget = item },
                                            )
                                            HorizontalDivider()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    editTarget?.let { item ->
        EditItemDialog(
            item = item,
            showLocation = state.isShopping,
            onDismiss = { editTarget = null },
            onSave = { name, details, location ->
                editTarget = null
                viewModel.updateItem(item, name, details, location)
            },
        )
    }

    if (showShare) {
        ShareSheet(
            listId = viewModel.listId,
            listName = state.list?.listName.orEmpty(),
            shares = state.shares,
            generatedLink = state.generatedLink,
            isBusy = state.isSharing,
            onGenerate = viewModel::createShare,
            onDismiss = {
                showShare = false
                viewModel.clearGeneratedLink()
            },
        )
    }
}

@Composable
private fun sectionTitle(kind: SectionKind): String? = when (kind) {
    is SectionKind.Location -> kind.name ?: stringResource(R.string.item_no_location)
    SectionKind.Pending -> null
    SectionKind.Completed -> stringResource(R.string.section_completed)
}

@Composable
private fun AddItemBar(
    draft: ItemDraft,
    showLocation: Boolean,
    nameSuggestions: List<String>,
    locationSuggestions: List<String>,
    onNameChange: (String) -> Unit,
    onLocationChange: (String) -> Unit,
    onAdd: () -> Unit,
) {
    val visibleNames = nameSuggestions.filter { !it.equals(draft.name, ignoreCase = true) }.take(8)
    val visibleLocations = if (showLocation) {
        locationSuggestions.filter { !it.equals(draft.location, ignoreCase = true) }.take(8)
    } else emptyList()

    Surface(tonalElevation = 3.dp, modifier = Modifier.imePadding()) {
        Column(Modifier.padding(horizontal = 12.dp, vertical = 8.dp)) {
            if (visibleNames.isNotEmpty()) {
                LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    items(visibleNames) { name ->
                        SuggestionChip(onClick = { onNameChange(name) }, label = { Text(name) })
                    }
                }
            }
            if (visibleLocations.isNotEmpty()) {
                LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    items(visibleLocations) { location ->
                        SuggestionChip(onClick = { onLocationChange(location) }, label = { Text(location) })
                    }
                }
            }
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = draft.name,
                    onValueChange = onNameChange,
                    placeholder = { Text(stringResource(R.string.add_item_hint)) },
                    singleLine = true,
                    modifier = Modifier.weight(1f),
                )
                if (showLocation) {
                    OutlinedTextField(
                        value = draft.location,
                        onValueChange = onLocationChange,
                        placeholder = { Text(stringResource(R.string.item_store_location)) },
                        singleLine = true,
                        modifier = Modifier.weight(1f),
                    )
                }
                IconButton(onClick = onAdd, enabled = draft.name.isNotBlank()) {
                    Icon(Icons.Filled.Add, stringResource(R.string.action_add))
                }
            }
        }
    }
}

@Composable
private fun EditItemDialog(
    item: ListItemEntity,
    showLocation: Boolean,
    onDismiss: () -> Unit,
    onSave: (name: String, details: String?, location: String?) -> Unit,
) {
    var name by remember { mutableStateOf(item.itemName) }
    var details by remember { mutableStateOf(item.itemDetails.orEmpty()) }
    var location by remember { mutableStateOf(item.storeLocation.orEmpty()) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(stringResource(R.string.edit_item)) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name, onValueChange = { name = it }, singleLine = true,
                    label = { Text(stringResource(R.string.item_name)) },
                    modifier = Modifier.fillMaxWidth(),
                )
                OutlinedTextField(
                    value = details, onValueChange = { details = it },
                    label = { Text(stringResource(R.string.item_details)) },
                    modifier = Modifier.fillMaxWidth(),
                )
                if (showLocation) {
                    OutlinedTextField(
                        value = location, onValueChange = { location = it }, singleLine = true,
                        label = { Text(stringResource(R.string.item_store_location)) },
                        modifier = Modifier.fillMaxWidth(),
                    )
                }
            }
        },
        confirmButton = {
            TextButton(enabled = name.isNotBlank(), onClick = { onSave(name, details, location) }) {
                Text(stringResource(R.string.action_save))
            }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text(stringResource(R.string.action_cancel)) } },
    )
}