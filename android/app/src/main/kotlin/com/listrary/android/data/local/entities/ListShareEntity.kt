package com.listrary.android.data.local.entities

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(tableName = "list_shares", indices = [Index("listId")])
data class ListShareEntity(
    @PrimaryKey val listShareId: Int,
    val listId: Int,
    val token: String,
    val permission: String,
    val ownerId: String,
    val recipientId: String?,
    val createdDate: String,
    val isAssigned: Boolean,
)