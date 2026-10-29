package com.listrary.android.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class ListShareDto(
    val listShareId: Int,
    val listId: Int,
    val token: String,
    val permission: String = "View",
    val ownerId: String = "",
    val recipientId: String? = null,
    val createdDate: String = "",
    val isAssigned: Boolean = false,
)

@Serializable
data class ShareRequestDto(val permission: String)

@Serializable
data class ShareResponseDto(
    val listShare: ListShareDto,
    val shareLink: String,
)