package com.listrary.android.data.local.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Transaction
import com.listrary.android.data.local.entities.ListShareEntity
import kotlinx.coroutines.flow.Flow

@Dao
abstract class ListShareDao {
    @Query("SELECT * FROM list_shares WHERE listId = :listId ORDER BY createdDate DESC")
    abstract fun observeShares(listId: Int): Flow<List<ListShareEntity>>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsert(share: ListShareEntity)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsertAll(shares: List<ListShareEntity>)

    @Query("DELETE FROM list_shares WHERE listId = :listId")
    abstract suspend fun deleteForList(listId: Int)

    @Transaction
    open suspend fun replaceForList(listId: Int, shares: List<ListShareEntity>) {
        deleteForList(listId)
        upsertAll(shares)
    }
}