package com.listrary.android.data.local.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Transaction
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.CircleInvitationEntity
import com.listrary.android.data.local.entities.CircleMemberEntity
import kotlinx.coroutines.flow.Flow

@Dao
abstract class CircleDao {
    @Query("SELECT * FROM circles ORDER BY name COLLATE NOCASE")
    abstract fun observeCircles(): Flow<List<CircleEntity>>

    @Query("SELECT * FROM circles WHERE circleId = :id")
    abstract fun observeCircle(id: Int): Flow<CircleEntity?>

    @Query("SELECT * FROM circle_members WHERE circleId = :circleId ORDER BY joinedDate")
    abstract fun observeMembers(circleId: Int): Flow<List<CircleMemberEntity>>

    @Query("SELECT * FROM circle_invitations WHERE circleId = :circleId AND isIncoming = 0 ORDER BY createdDate DESC")
    abstract fun observeInvitations(circleId: Int): Flow<List<CircleInvitationEntity>>

    @Query("SELECT * FROM circle_invitations WHERE isIncoming = 1 AND accepted = 0 ORDER BY createdDate DESC")
    abstract fun observeIncomingInvitations(): Flow<List<CircleInvitationEntity>>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsertCircles(circles: List<CircleEntity>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsertMembers(members: List<CircleMemberEntity>)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsertInvitations(invitations: List<CircleInvitationEntity>)

    @Query("DELETE FROM circles")
    abstract suspend fun deleteAllCircles()

    @Query("DELETE FROM circles WHERE circleId = :id")
    abstract suspend fun deleteCircleRow(id: Int)

    @Query("DELETE FROM circle_members WHERE circleId = :id")
    abstract suspend fun deleteMembersOf(id: Int)

    @Query("DELETE FROM circle_members WHERE circleId = :id AND userId = :userId")
    abstract suspend fun deleteMember(id: Int, userId: String)

    @Query("DELETE FROM circle_invitations WHERE circleId = :id AND isIncoming = 0")
    abstract suspend fun deleteOutgoingInvitationsOf(id: Int)

    @Query("DELETE FROM circle_invitations WHERE circleInvitationId = :invitationId")
    abstract suspend fun deleteInvitation(invitationId: Int)

    @Query("DELETE FROM circle_invitations WHERE isIncoming = 1")
    abstract suspend fun deleteIncomingInvitations()

    @Transaction
    open suspend fun replaceCircles(
        circles: List<CircleEntity>,
        incoming: List<CircleInvitationEntity>,
    ) {
        deleteAllCircles()
        upsertCircles(circles)
        deleteIncomingInvitations()
        upsertInvitations(incoming)
    }

    @Transaction
    open suspend fun replaceCircleDetail(
        circle: CircleEntity,
        members: List<CircleMemberEntity>,
        invitations: List<CircleInvitationEntity>,
    ) {
        upsertCircles(listOf(circle))
        deleteMembersOf(circle.circleId)
        upsertMembers(members)
        deleteOutgoingInvitationsOf(circle.circleId)
        upsertInvitations(invitations)
    }

    @Transaction
    open suspend fun deleteCircleCompletely(id: Int) {
        deleteMembersOf(id)
        deleteOutgoingInvitationsOf(id)
        deleteCircleRow(id)
    }
}