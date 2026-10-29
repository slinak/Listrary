package com.listrary.android.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class ListItemDto(
    val listItemId: Int,
    val listId: Int,
    val itemName: String,
    val itemDetails: String? = null,
    val storeLocation: String? = null,
    val isCompleted: Boolean = false,
    val sortOrder: Int = 0,
    val createdDate: String = "",
    val lastUpdatedDate: String = "",
    val addedDate: String = "",
)

@Serializable
data class ItemRequestDto(
    val itemName: String,
    val itemDetails: String? = null,
    val storeLocation: String? = null,
)

@Serializable
data class UpdateItemRequestDto(
    val itemName: String,
    val itemDetails: String? = null,
    val storeLocation: String? = null,
    val isCompleted: Boolean = false,
)

@Serializable
data class ItemResponseDto(
    val item: ListItemDto,
    val warning: String? = null,
)