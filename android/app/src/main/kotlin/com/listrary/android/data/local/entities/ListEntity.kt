package com.listrary.android.data.local.entities

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "lists")
data class ListEntity(
    @PrimaryKey val listId: Int,
    val listName: String,
    val listType: String,
    val ownerId: String,
    val circleId: Int?,
    val circleName: String?,
    val createdDate: String,
    val lastUpdatedDate: String,
    val isCompleted: Boolean,
    val isDeleted: Boolean,
    val strictStoreLocations: Boolean,
    val itemCount: Int,
    /** Set from ListDetail; summaries default to true (owner/circle access). */
    val canEdit: Boolean = true,
    /** Share token used to reach this list, if opened through a share link. */
    val shareToken: String? = null,
    /** True when the row came from GET /lists (shown on the Lists screen). */
    val isInSummary: Boolean = true,
)