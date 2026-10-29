package com.listrary.android.ui.common

import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.Checkbox
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextDecoration
import com.listrary.android.R
import com.listrary.android.data.local.entities.ListItemEntity

/** A single to-do/shopping item: checkbox, name/details, and an edit button when editable. */
@Composable
fun ListItemRow(
    item: ListItemEntity,
    canEdit: Boolean,
    onToggle: () -> Unit,
    onEdit: () -> Unit,
    modifier: Modifier = Modifier,
) {
    ListItem(
        modifier = modifier,
        leadingContent = {
            Checkbox(checked = item.isCompleted, onCheckedChange = { onToggle() }, enabled = canEdit)
        },
        headlineContent = {
            Text(
                item.itemName,
                textDecoration = if (item.isCompleted) TextDecoration.LineThrough else null,
                color = if (item.isCompleted) MaterialTheme.colorScheme.onSurfaceVariant
                else MaterialTheme.colorScheme.onSurface,
            )
        },
        supportingContent = if (!item.itemDetails.isNullOrBlank()) {
            { Text(item.itemDetails.orEmpty()) }
        } else null,
        trailingContent = if (canEdit) {
            {
                Row {
                    IconButton(onClick = onEdit) {
                        Icon(Icons.Filled.Edit, contentDescription = stringResource(R.string.edit_item))
                    }
                }
            }
        } else null,
    )
}