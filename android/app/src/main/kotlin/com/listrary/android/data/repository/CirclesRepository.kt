package com.listrary.android.data.repository

import com.listrary.android.data.local.dao.CircleDao
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.CircleInvitationEntity
import com.listrary.android.data.local.entities.CircleMemberEntity
import com.listrary.android.data.remote.ListraryApiService
import com.listrary.android.data.remote.dto.CreateCircleRequestDto
import com.listrary.android.data.remote.dto.InviteRequestDto
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

interface CirclesRepository {
    fun observeCircles(): Flow<List<CircleEntity>>
    fun observeIncomingInvitations(): Flow<List<CircleInvitationEntity>>
    fun observeCircle(circleId: Int): Flow<CircleEntity?>
    fun observeMembers(circleId: Int): Flow<List<CircleMemberEntity>>
    fun observeInvitations(circleId: Int): Flow<List<CircleInvitationEntity>>

    suspend fun refresh(): Result<Unit>
    suspend fun refreshCircle(circleId: Int): Result<Unit>
    suspend fun createCircle(name: String): Result<Unit>
    suspend fun invite(circleId: Int, recipientEmail: String?, recipientId: String?): Result<Unit>
    suspend fun removeMember(circleId: Int, userId: String): Result<Unit>
    suspend fun leave(circleId: Int): Result<Unit>
    suspend fun acceptInvitation(invitationId: Int): Result<Unit>
    suspend fun rejectInvitation(invitationId: Int): Result<Unit>
    suspend fun cancelInvitation(invitationId: Int): Result<Unit>
    suspend fun deleteCircle(circleId: Int): Result<Unit>
}

class CirclesRepositoryImpl @Inject constructor(
    private val api: ListraryApiService,
    private val dao: CircleDao,
) : CirclesRepository {

    override fun observeCircles() = dao.observeCircles()
    override fun observeIncomingInvitations() = dao.observeIncomingInvitations()
    override fun observeCircle(circleId: Int) = dao.observeCircle(circleId)
    override fun observeMembers(circleId: Int) = dao.observeMembers(circleId)
    override fun observeInvitations(circleId: Int) = dao.observeInvitations(circleId)

    override suspend fun refresh() = apiCall {
        val response = api.getCircles()
        dao.replaceCircles(
            response.circles.map { it.toEntity() },
            response.pendingInvitations.map { it.toEntity(isIncoming = true) },
        )
        // Keep cached members/invitations for circles that include them in the summary payload.
        response.circles.filter { it.members.isNotEmpty() || it.invitations.isNotEmpty() }.forEach { c ->
            dao.replaceCircleDetail(
                c.toEntity(),
                c.members.map { it.toEntity() },
                c.invitations.map { it.toEntity(isIncoming = false) },
            )
        }
    }

    override suspend fun refreshCircle(circleId: Int) = apiCall {
        val c = api.getCircle(circleId)
        dao.replaceCircleDetail(
            c.toEntity(),
            c.members.map { it.toEntity() },
            c.invitations.map { it.toEntity(isIncoming = false) },
        )
    }

    override suspend fun createCircle(name: String) = apiCall {
        api.createCircle(CreateCircleRequestDto(name.trim()))
        refresh().getOrThrow()
    }

    override suspend fun invite(circleId: Int, recipientEmail: String?, recipientId: String?) = apiCall {
        api.invite(circleId, InviteRequestDto(recipientEmail, recipientId))
        refreshCircle(circleId).getOrThrow()
    }

    override suspend fun removeMember(circleId: Int, userId: String) = apiCall {
        api.removeMember(circleId, userId)
        dao.deleteMember(circleId, userId)
    }

    override suspend fun leave(circleId: Int) = apiCall {
        api.leaveCircle(circleId)
        dao.deleteCircleCompletely(circleId)
    }

    override suspend fun acceptInvitation(invitationId: Int) = apiCall {
        api.acceptInvitation(invitationId)
        refresh().getOrThrow()
    }

    override suspend fun rejectInvitation(invitationId: Int) = apiCall {
        api.rejectInvitation(invitationId)
        dao.deleteInvitation(invitationId)
    }

    override suspend fun cancelInvitation(invitationId: Int) = apiCall {
        api.cancelInvitation(invitationId)
        dao.deleteInvitation(invitationId)
    }

    override suspend fun deleteCircle(circleId: Int) = apiCall {
        api.deleteCircle(circleId)
        dao.deleteCircleCompletely(circleId)
    }
}