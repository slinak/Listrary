package com.listrary.android.data.local.entities

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(tableName = "circles")
data class CircleEntity(
    @PrimaryKey val circleId: Int,
    val name: String,
    val ownerId: String,
    val createdDate: String,
)

@Entity(tableName = "circle_members", indices = [Index("circleId")])
data class CircleMemberEntity(
    @PrimaryKey val circleMemberId: Int,
    val circleId: Int,
    val userId: String,
    val joinedDate: String,
)

@Entity(tableName = "circle_invitations", indices = [Index("circleId")])
data class CircleInvitationEntity(
    @PrimaryKey val circleInvitationId: Int,
    val circleId: Int,
    val inviterId: String,
    val recipientEmail: String?,
    val recipientId: String?,
    val token: String,
    val accepted: Boolean,
    val createdDate: String,
    val circleName: String?,
    /** True for invitations addressed to the current user (pendingInvitations). */
    val isIncoming: Boolean,
)