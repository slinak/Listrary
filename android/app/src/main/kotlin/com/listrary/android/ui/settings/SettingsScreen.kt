package com.listrary.android.ui.settings

import androidx.appcompat.app.AppCompatDelegate
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.selection.selectable
import androidx.compose.foundation.selection.selectableGroup
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.core.os.LocaleListCompat
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.listrary.android.BuildConfig
import com.listrary.android.R
import com.listrary.android.auth.AuthState

private data class Language(val tag: String, val labelRes: Int)

private val languages = listOf(
    Language("en", R.string.language_english),
    Language("es", R.string.language_spanish),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(viewModel: SettingsViewModel = hiltViewModel()) {
    val authState by viewModel.authState.collectAsStateWithLifecycle()
    var currentTag by remember {
        mutableStateOf(AppCompatDelegate.getApplicationLocales().get(0)?.language ?: "en")
    }

    Scaffold(topBar = { TopAppBar(title = { Text(stringResource(R.string.settings_title)) }) }) { padding ->
        Column(Modifier.padding(padding).fillMaxSize().padding(16.dp)) {
            Text(stringResource(R.string.settings_language), style = MaterialTheme.typography.titleMedium)
            Column(Modifier.selectableGroup()) {
                languages.forEach { language ->
                    Row(
                        Modifier.fillMaxWidth()
                            .selectable(
                                selected = currentTag == language.tag,
                                role = Role.RadioButton,
                                onClick = {
                                    currentTag = language.tag
                                    AppCompatDelegate.setApplicationLocales(
                                        LocaleListCompat.forLanguageTags(language.tag),
                                    )
                                },
                            )
                            .padding(vertical = 4.dp),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        RadioButton(selected = currentTag == language.tag, onClick = null)
                        Text(stringResource(language.labelRes), Modifier.padding(start = 12.dp))
                    }
                }
            }

            Text(
                stringResource(R.string.settings_account),
                style = MaterialTheme.typography.titleMedium,
                modifier = Modifier.padding(top = 24.dp),
            )
            (authState as? AuthState.SignedIn)?.let {
                Text(stringResource(R.string.settings_signed_in_as, it.displayName), Modifier.padding(vertical = 4.dp))
            }
            OutlinedButton(onClick = viewModel::signOut, modifier = Modifier.padding(top = 8.dp)) {
                Text(stringResource(R.string.sign_out))
            }

            Text(
                stringResource(R.string.settings_version, BuildConfig.VERSION_NAME),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 24.dp),
            )
        }
    }
}