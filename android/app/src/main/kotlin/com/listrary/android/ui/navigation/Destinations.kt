package com.listrary.android.ui.navigation

import android.net.Uri
import com.listrary.android.BuildConfig

object Destinations {
    const val SIGN_IN = "signin"
    const val LISTS = "lists"
    const val CIRCLES = "circles"
    const val SETTINGS = "settings"

    const val ARG_LIST_ID = "listId"
    const val ARG_SHARE_TOKEN = "shareToken"
    const val ARG_CIRCLE_ID = "circleId"

    const val LIST_DETAIL = "list/{$ARG_LIST_ID}?$ARG_SHARE_TOKEN={$ARG_SHARE_TOKEN}"
    const val CIRCLE_DETAIL = "circle/{$ARG_CIRCLE_ID}"

    val topLevel = setOf(LISTS, CIRCLES, SETTINGS)

    fun listDetail(listId: Int, shareToken: String? = null): String =
        "list/$listId" + if (shareToken.isNullOrBlank()) "" else "?$ARG_SHARE_TOKEN=${Uri.encode(shareToken)}"

    fun circleDetail(circleId: Int) = "circle/$circleId"

    /** Web share URL (also an App Link): https://HOST/Lists/Manage/{listId}?shareToken={token} */
    val listAppLink = "https://${BuildConfig.DEEP_LINK_HOST}/Lists/Manage/{$ARG_LIST_ID}?$ARG_SHARE_TOKEN={$ARG_SHARE_TOKEN}"

    /** Fallback custom scheme: listrary://list/{listId}?shareToken={token} */
    const val LIST_CUSTOM_SCHEME_LINK = "listrary://list/{$ARG_LIST_ID}?$ARG_SHARE_TOKEN={$ARG_SHARE_TOKEN}"
}