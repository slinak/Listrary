package com.listrary.android.data.remote.dto

import kotlinx.serialization.Serializable

@Serializable
data class CircleMemberDto(
    val circleMemberId: Int,
    val circleId: Int,
    val userId: String,
    val joinedDate: String = "",
)

@Serializable
data class CircleInvitationDto(
    val circleInvitationId: Int,
    val circleId: Int,
    val inviterId: String = "",
    val recipientEmail: String? = null,
    val recipientId: String? = null,
    val token: String = "",
    val accepted: Boolean = false,
    val createdDate: String = "",
    // Optional extra the backend may include on incoming invitations so the UI can show a name.
    val circleName: String? = null,
)

@Serializable
data class CircleDto(
    val circleId: Int,
    val name: String,
    val ownerId: String = "",
    val createdDate: String = "",
    val members: List<CircleMemberDto> = emptyList(),
    val invitations: List<CircleInvitationDto> = emptyList(),
)

@Serializable
data class CirclesResponseDto(
    val circles: List<CircleDto> = emptyList(),
    val pendingInvitations: List<CircleInvitationDto> = emptyList(),
)

@Serializable
data class CreateCircleRequestDto(val name: String)

@Serializable
data class InviteRequestDto(
    val recipientEmail: String? = null,
    val recipientId: String? = null,
)