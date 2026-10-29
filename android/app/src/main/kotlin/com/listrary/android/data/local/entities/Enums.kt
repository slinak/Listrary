package com.listrary.android.data.local.entities

enum class ListType(val apiValue: String) {
    Shopping("Shopping"),
    ToDo("ToDo"),
    WishList("WishList"),
    Other("Other");

    companion object {
        fun fromApi(value: String?): ListType =
            entries.firstOrNull { it.apiValue.equals(value, ignoreCase = true) } ?: Other
    }
}

enum class SharePermission(val apiValue: String) {
    View("View"),
    Edit("Edit");

    companion object {
        fun fromApi(value: String?): SharePermission =
            entries.firstOrNull { it.apiValue.equals(value, ignoreCase = true) } ?: View
    }
}