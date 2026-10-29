package com.listrary.android.ui.common

import androidx.annotation.StringRes
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Checklist
import androidx.compose.material.icons.filled.CardGiftcard
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.ui.graphics.vector.ImageVector
import com.listrary.android.R
import com.listrary.android.data.local.entities.ListType
import com.listrary.android.data.local.entities.SharePermission

@get:StringRes
val ListType.labelRes: Int
    get() = when (this) {
        ListType.Shopping -> R.string.list_type_shopping
        ListType.ToDo -> R.string.list_type_todo
        ListType.WishList -> R.string.list_type_wishlist
        ListType.Other -> R.string.list_type_other
    }

val ListType.icon: ImageVector
    get() = when (this) {
        ListType.Shopping -> Icons.Filled.ShoppingCart
        ListType.ToDo -> Icons.Filled.Checklist
        ListType.WishList -> Icons.Filled.CardGiftcard
        ListType.Other -> Icons.Filled.List
    }

@get:StringRes
val SharePermission.labelRes: Int
    get() = when (this) {
        SharePermission.View -> R.string.share_permission_view
        SharePermission.Edit -> R.string.share_permission_edit
    }