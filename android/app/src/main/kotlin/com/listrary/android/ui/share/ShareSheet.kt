package com.listrary.android.ui.share

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.widget.Toast
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.material.icons.filled.Share
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.listrary.android.BuildConfig
import com.listrary.android.R
import com.listrary.android.data.local.entities.ListShareEntity
import com.listrary.android.data.local.entities.SharePermission
import com.listrary.android.ui.common.labelRes

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ShareSheet(
    listId: Int,
    listName: String,
    shares: List<ListShareEntity>,
    generatedLink: String?,
    isBusy: Boolean,
    onGenerate: (SharePermission) -> Unit,
    onDismiss: () -> Unit,
) {
    val context = LocalContext.current
    var permission by remember { mutableStateOf(SharePermission.View) }

    ModalBottomSheet(onDismissRequest = onDismiss, sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)) {
        Column(Modifier.padding(horizontal = 16.dp).padding(bottom = 24.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Text(stringResource(R.string.share_title), style = MaterialTheme.typography.titleLarge)
            Text(stringResource(R.string.share_permission), style = MaterialTheme.typography.labelLarge)
            SingleChoiceSegmentedButtonRow(Modifier.fillMaxWidth()) {
                SharePermission.entries.forEachIndexed { index, option ->
                    SegmentedButton(
                        selected = permission == option,
                        onClick = { permission = option },
                        shape = SegmentedButtonDefaults.itemShape(index, SharePermission.entries.size),
                    ) { Text(stringResource(option.labelRes)) }
                }
            }
            Button(onClick = { onGenerate(permission) }, enabled = !isBusy, modifier = Modifier.fillMaxWidth()) {
                if (isBusy) CircularProgressIndicator(Modifier.padding(end = 8.dp).size(16.dp))
                Text(stringResource(R.string.share_generate_link))
            }

            if (generatedLink != null) {
                Text(generatedLink, style = MaterialTheme.typography.bodyMedium)
                LinkActions(context, listName, generatedLink)
            }

            HorizontalDivider()
            Text(stringResource(R.string.share_active_shares), style = MaterialTheme.typography.titleMedium)
            if (shares.isEmpty()) {
                Text(stringResource(R.string.share_none), color = MaterialTheme.colorScheme.onSurfaceVariant)
            } else {
                LazyColumn {
                    items(shares, key = { it.listShareId }) { share ->
                        val link = buildShareLink(listId, share.token)
                        ListItem(
                            headlineContent = {
                                Text(stringResource(SharePermission.fromApi(share.permission).labelRes))
                            },
                            supportingContent = {
                                Text(
                                    stringResource(
                                        if (share.isAssigned) R.string.share_assigned else R.string.share_unclaimed,
                                    ),
                                )
                            },
                            trailingContent = {
                                Row {
                                    IconButton(onClick = { copyLink(context, link) }) {
                                        Icon(Icons.Filled.ContentCopy, stringResource(R.string.share_copy_link))
                                    }
                                    IconButton(onClick = { sendLink(context, listName, link) }) {
                                        Icon(Icons.Filled.Share, stringResource(R.string.share_send_link))
                                    }
                                }
                            },
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun LinkActions(context: Context, listName: String, link: String) {
    Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
        OutlinedButton(onClick = { copyLink(context, link) }) {
            Icon(Icons.Filled.ContentCopy, null, Modifier.padding(end = 4.dp))
            Text(stringResource(R.string.share_copy_link))
        }
        Button(onClick = { sendLink(context, listName, link) }) {
            Icon(Icons.Filled.Share, null, Modifier.padding(end = 4.dp))
            Text(stringResource(R.string.share_send_link))
        }
    }
}

/** Same shape as the web app's share URL so the App Link opens this screen. */
fun buildShareLink(listId: Int, token: String): String =
    "https://${BuildConfig.DEEP_LINK_HOST}/Lists/Manage/$listId?shareToken=$token"

private fun copyLink(context: Context, link: String) {
    val clipboard = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
    clipboard.setPrimaryClip(ClipData.newPlainText("Listrary", link))
    Toast.makeText(context, R.string.share_link_copied, Toast.LENGTH_SHORT).show()
}

private fun sendLink(context: Context, listName: String, link: String) {
    val intent = Intent(Intent.ACTION_SEND).apply {
        type = "text/plain"
        putExtra(Intent.EXTRA_TEXT, context.getString(R.string.share_message, listName, link))
    }
    context.startActivity(Intent.createChooser(intent, context.getString(R.string.share_send_link)))
}