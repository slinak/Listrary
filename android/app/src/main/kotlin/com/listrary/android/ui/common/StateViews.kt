package com.listrary.android.ui.common

import androidx.annotation.StringRes
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.listrary.android.R
import retrofit2.HttpException
import java.io.IOException

@Composable
fun LoadingState(modifier: Modifier = Modifier) {
    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

@Composable
fun ErrorState(@StringRes messageRes: Int, onRetry: () -> Unit, modifier: Modifier = Modifier) {
    Column(
        modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(
            stringResource(messageRes),
            style = MaterialTheme.typography.bodyLarge,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.error,
        )
        Button(onClick = onRetry, modifier = Modifier.padding(top = 16.dp)) {
            Text(stringResource(R.string.action_retry))
        }
    }
}

@Composable
fun EmptyState(@StringRes messageRes: Int, modifier: Modifier = Modifier) {
    Box(modifier.fillMaxSize().padding(24.dp), contentAlignment = Alignment.Center) {
        Text(
            stringResource(messageRes),
            style = MaterialTheme.typography.bodyLarge,
            textAlign = TextAlign.Center,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

/** Maps a failed API call to a user-facing string resource. */
@StringRes
fun Throwable.toMessageRes(): Int = when (this) {
    is HttpException -> when (code()) {
        401 -> R.string.error_unauthorized
        403 -> R.string.error_forbidden
        404 -> R.string.error_not_found
        else -> R.string.error_generic
    }
    is IOException -> R.string.error_network
    else -> R.string.error_generic
}

/** Extracts the backend's `{"error": "..."}` body text from a failed API call, when present. */
fun HttpException.backendErrorMessage(): String? = try {
    response()?.errorBody()?.string()
        ?.let { org.json.JSONObject(it) }
        ?.optString("error")
        ?.takeIf { it.isNotBlank() }
} catch (e: Exception) {
    null
}