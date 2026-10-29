package com.listrary.android.data.repository

import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.CircleInvitationEntity
import com.listrary.android.data.local.entities.CircleMemberEntity
import com.listrary.android.data.local.entities.ListEntity
import com.listrary.android.data.local.entities.ListItemEntity
import com.listrary.android.data.local.entities.ListShareEntity
import com.listrary.android.data.remote.dto.CircleDto
import com.listrary.android.data.remote.dto.CircleInvitationDto
import com.listrary.android.data.remote.dto.CircleMemberDto
import com.listrary.android.data.remote.dto.ListItemDto
import com.listrary.android.data.remote.dto.ListShareDto
import com.listrary.android.data.remote.dto.ListSummaryDto
import kotlin.coroutines.cancellation.CancellationException

fun ListSummaryDto.toEntity(
    circleIdOverride: Int? = circleId,
    circleNameOverride: String? = circleName,
    canEdit: Boolean = true,
    shareToken: String? = null,
    isInSummary: Boolean = true,
) = ListEntity(
    listId = listId,
    listName = listName,
    listType = listType,
    ownerId = ownerId,
    circleId = circleIdOverride,
    circleName = circleNameOverride,
    createdDate = createdDate,
    lastUpdatedDate = lastUpdatedDate,
    isCompleted = isCompleted,
    isDeleted = isDeleted,
    strictStoreLocations = strictStoreLocations,
    itemCount = itemCount,
    canEdit = canEdit,
    shareToken = shareToken,
    isInSummary = isInSummary,
)

fun ListItemDto.toEntity() = ListItemEntity(
    listItemId, listId, itemName, itemDetails, storeLocation, isCompleted,
    sortOrder, createdDate, lastUpdatedDate, addedDate,
)

fun ListShareDto.toEntity() = ListShareEntity(
    listShareId, listId, token, permission, ownerId, recipientId, createdDate, isAssigned,
)

fun CircleDto.toEntity() = CircleEntity(circleId, name, ownerId, createdDate)

fun CircleMemberDto.toEntity() = CircleMemberEntity(circleMemberId, circleId, userId, joinedDate)

fun CircleInvitationDto.toEntity(isIncoming: Boolean) = CircleInvitationEntity(
    circleInvitationId, circleId, inviterId, recipientEmail, recipientId, token,
    accepted, createdDate, circleName, isIncoming,
)

/** Runs an API call, converting failures into Result while keeping coroutine cancellation intact. */
suspend inline fun <T> apiCall(crossinline block: suspend () -> T): Result<T> =
    try {
        Result.success(block())
    } catch (e: CancellationException) {
        throw e
    } catch (e: Exception) {
        Result.failure(e)
    }