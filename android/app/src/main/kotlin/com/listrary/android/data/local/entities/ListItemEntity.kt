package com.listrary.android.data.local.entities

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(tableName = "list_items", indices = [Index("listId")])
data class ListItemEntity(
    @PrimaryKey val listItemId: Int,
    val listId: Int,
    val itemName: String,
    val itemDetails: String?,
    val storeLocation: String?,
    val isCompleted: Boolean,
    val sortOrder: Int,
    val createdDate: String,
    val lastUpdatedDate: String,
    val addedDate: String,
)