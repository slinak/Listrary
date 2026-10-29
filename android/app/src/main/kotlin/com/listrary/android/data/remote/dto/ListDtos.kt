package com.listrary.android.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class ListSummaryDto(
    val listId: Int,
    val listName: String,
    val listType: String = "Other",
    val ownerId: String = "",
    val circleId: Int? = null,
    val circleName: String? = null,
    val createdDate: String = "",
    val lastUpdatedDate: String = "",
    val isCompleted: Boolean = false,
    val isDeleted: Boolean = false,
    val strictStoreLocations: Boolean = false,
    val itemCount: Int = 0,
)

@Serializable
data class CircleRefDto(val circleId: Int, val name: String)

@Serializable
data class CircleGroupDto(
    val circle: CircleRefDto,
    val lists: List<ListSummaryDto> = emptyList(),
)

@Serializable
data class ListsResponseDto(
    val circleGroups: List<CircleGroupDto> = emptyList(),
    val otherLists: List<ListSummaryDto> = emptyList(),
)

@Serializable
data class ListDetailDto(
    val list: ListSummaryDto,
    val items: List<ListItemDto> = emptyList(),
    val canEdit: Boolean = false,
    val shareToken: String? = null,
    val activeShares: List<ListShareDto> = emptyList(),
)

@Serializable
data class ListRequestDto(
    val listName: String,
    val listType: String,
    val circleId: Int? = null,
    val strictStoreLocations: Boolean = false,
)