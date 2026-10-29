package com.listrary.android.data.repository

import com.listrary.android.data.local.dao.ListItemDao
import com.listrary.android.data.local.entities.ListItemEntity
import com.listrary.android.data.remote.ListraryApiService
import com.listrary.android.data.remote.dto.ItemRequestDto
import com.listrary.android.data.remote.dto.UpdateItemRequestDto
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

interface ListItemsRepository {
    fun observeItems(listId: Int): Flow<List<ListItemEntity>>

    /** Each mutation returns the optional server warning (e.g. similar store location). */
    suspend fun addItem(listId: Int, name: String, details: String?, location: String?, shareToken: String?): Result<String?>
    suspend fun updateItem(
        listId: Int, itemId: Int, name: String, details: String?, location: String?,
        isCompleted: Boolean, shareToken: String?,
    ): Result<String?>
    suspend fun deleteItem(listId: Int, itemId: Int, shareToken: String?): Result<Unit>
    suspend fun toggleItem(listId: Int, itemId: Int, shareToken: String?): Result<Unit>
    suspend fun markAllIncomplete(listId: Int, shareToken: String?): Result<Unit>
    suspend fun suggestNames(listId: Int, query: String, shareToken: String?): Result<List<String>>
    suspend fun suggestLocations(listId: Int, itemName: String, query: String, shareToken: String?): Result<List<String>>
}

class ListItemsRepositoryImpl @Inject constructor(
    private val api: ListraryApiService,
    private val dao: ListItemDao,
) : ListItemsRepository {

    override fun observeItems(listId: Int) = dao.observeItems(listId)

    override suspend fun addItem(listId: Int, name: String, details: String?, location: String?, shareToken: String?) =
        apiCall {
            val response = api.addItem(
                listId,
                ItemRequestDto(name.trim(), details.blankToNull(), location.blankToNull()),
                shareToken,
            )
            dao.upsert(response.item.toEntity())
            response.warning
        }

    override suspend fun updateItem(
        listId: Int, itemId: Int, name: String, details: String?, location: String?,
        isCompleted: Boolean, shareToken: String?,
    ) = apiCall {
        val response = api.updateItem(
            listId, itemId,
            UpdateItemRequestDto(name.trim(), details.blankToNull(), location.blankToNull(), isCompleted),
            shareToken,
        )
        dao.upsert(response.item.toEntity())
        response.warning
    }

    override suspend fun deleteItem(listId: Int, itemId: Int, shareToken: String?) = apiCall {
        api.deleteItem(listId, itemId, shareToken)
        dao.delete(itemId)
    }

    override suspend fun toggleItem(listId: Int, itemId: Int, shareToken: String?) = apiCall {
        dao.upsert(api.toggleItem(listId, itemId, shareToken).toEntity())
    }

    override suspend fun markAllIncomplete(listId: Int, shareToken: String?) = apiCall {
        api.markAllIncomplete(listId, shareToken)
        dao.markAllIncomplete(listId)
    }

    override suspend fun suggestNames(listId: Int, query: String, shareToken: String?) =
        apiCall { api.suggestNames(listId, query, shareToken) }

    override suspend fun suggestLocations(listId: Int, itemName: String, query: String, shareToken: String?) =
        apiCall { api.suggestLocations(listId, itemName, query, shareToken) }

    private fun String?.blankToNull(): String? = this?.trim()?.takeIf { it.isNotEmpty() }
}