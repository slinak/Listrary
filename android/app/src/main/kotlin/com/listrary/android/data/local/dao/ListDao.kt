package com.listrary.android.data.local.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Transaction
import com.listrary.android.data.local.entities.ListEntity
import kotlinx.coroutines.flow.Flow

@Dao
abstract class ListDao {
    @Query("SELECT * FROM lists WHERE isInSummary = 1 AND isDeleted = 0 ORDER BY listName COLLATE NOCASE")
    abstract fun observeSummaries(): Flow<List<ListEntity>>

    @Query("SELECT * FROM lists WHERE listId = :id")
    abstract fun observeList(id: Int): Flow<ListEntity?>

    @Query("SELECT * FROM lists WHERE listId = :id")
    abstract suspend fun getList(id: Int): ListEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsert(list: ListEntity)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsertAll(lists: List<ListEntity>)

    @Query("DELETE FROM lists WHERE isInSummary = 1")
    abstract suspend fun deleteSummaries()

    @Query("DELETE FROM lists WHERE listId = :id")
    abstract suspend fun delete(id: Int)

    @Query("DELETE FROM list_items WHERE listId = :id")
    abstract suspend fun deleteItemsOf(id: Int)

    @Query("DELETE FROM list_shares WHERE listId = :id")
    abstract suspend fun deleteSharesOf(id: Int)

    @Transaction
    open suspend fun replaceSummaries(lists: List<ListEntity>) {
        deleteSummaries()
        upsertAll(lists)
    }

    @Transaction
    open suspend fun deleteCompletely(id: Int) {
        deleteItemsOf(id)
        deleteSharesOf(id)
        delete(id)
    }
}