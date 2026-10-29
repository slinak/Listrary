package com.listrary.android.data.local.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Transaction
import com.listrary.android.data.local.entities.ListItemEntity
import kotlinx.coroutines.flow.Flow

@Dao
abstract class ListItemDao {
    @Query("SELECT * FROM list_items WHERE listId = :listId ORDER BY sortOrder, listItemId")
    abstract fun observeItems(listId: Int): Flow<List<ListItemEntity>>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsert(item: ListItemEntity)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun upsertAll(items: List<ListItemEntity>)

    @Query("DELETE FROM list_items WHERE listId = :listId")
    abstract suspend fun deleteForList(listId: Int)

    @Query("DELETE FROM list_items WHERE listItemId = :itemId")
    abstract suspend fun delete(itemId: Int)

    @Query("UPDATE list_items SET isCompleted = 0 WHERE listId = :listId")
    abstract suspend fun markAllIncomplete(listId: Int)

    @Transaction
    open suspend fun replaceForList(listId: Int, items: List<ListItemEntity>) {
        deleteForList(listId)
        upsertAll(items)
    }
}