package com.listrary.android.ui.common

import androidx.annotation.StringRes
import android.content.Context

/**
 * A snackbar/toast message that is either a canned client string resource or literal text
 * (e.g. surfaced verbatim from a backend error/warning response body).
 */
sealed interface UiMessage {
    data class Res(@StringRes val id: Int) : UiMessage
    data class Text(val value: String) : UiMessage

    fun resolve(context: Context): String = when (this) {
        is Res -> context.getString(id)
        is Text -> value
    }
}

fun @receiver:StringRes Int.toUiMessage(): UiMessage = UiMessage.Res(this)
