package com.listrary.android.data.repository

import com.listrary.android.data.local.dao.ListDao
import com.listrary.android.data.local.dao.ListItemDao
import com.listrary.android.data.local.dao.ListShareDao
import com.listrary.android.data.local.entities.ListEntity
import com.listrary.android.data.local.entities.ListShareEntity
import com.listrary.android.data.local.entities.ListType
import com.listrary.android.data.local.entities.SharePermission
import com.listrary.android.data.remote.ListraryApiService
import com.listrary.android.data.remote.dto.ListRequestDto
import com.listrary.android.data.remote.dto.ShareRequestDto
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

data class CreatedShare(val share: ListShareEntity, val shareLink: String)

interface ListsRepository {
    fun observeSummaries(): Flow<List<ListEntity>>
    fun observeList(listId: Int): Flow<ListEntity?>
    fun observeShares(listId: Int): Flow<List<ListShareEntity>>

    /** Replaces the cached list summaries with GET /lists. */
    suspend fun refresh(): Result<Unit>

    /** Replaces cached list, items and shares with GET /lists/{id}. */
    suspend fun refreshDetail(listId: Int, shareToken: String?): Result<Unit>

    suspend fun createList(name: String, type: ListType, circleId: Int?, strict: Boolean): Result<Unit>
    suspend fun updateList(listId: Int, name: String, type: ListType, circleId: Int?, strict: Boolean): Result<Unit>
    suspend fun deleteList(listId: Int): Result<Unit>
    suspend fun createShare(listId: Int, permission: SharePermission): Result<CreatedShare>
}

class ListsRepositoryImpl @Inject constructor(
    private val api: ListraryApiService,
    private val listDao: ListDao,
    private val itemDao: ListItemDao,
    private val shareDao: ListShareDao,
) : ListsRepository {

    override fun observeSummaries() = listDao.observeSummaries()
    override fun observeList(listId: Int) = listDao.observeList(listId)
    override fun observeShares(listId: Int) = shareDao.observeShares(listId)

    override suspend fun refresh(): Result<Unit> = apiCall {
        val response = api.getLists()
        val entities = response.circleGroups.flatMap { group ->
            group.lists.map { it.toEntity(group.circle.circleId, group.circle.name) }
        } + response.otherLists.map { it.toEntity() }
        listDao.replaceSummaries(entities)
    }

    override suspend fun refreshDetail(listId: Int, shareToken: String?): Result<Unit> = apiCall {
        val detail = api.getList(listId, shareToken)
        val existing = listDao.getList(listId)
        listDao.upsert(
            detail.list.toEntity(
                canEdit = detail.canEdit,
                shareToken = shareToken,
                isInSummary = existing?.isInSummary ?: false,
            ),
        )
        itemDao.replaceForList(listId, detail.items.map { it.toEntity() })
        shareDao.replaceForList(listId, detail.activeShares.map { it.toEntity() })
    }

    override suspend fun createList(name: String, type: ListType, circleId: Int?, strict: Boolean) = apiCall {
        api.createList(ListRequestDto(name.trim(), type.apiValue, circleId, strict))
        refresh().getOrThrow()
    }

    override suspend fun updateList(
        listId: Int, name: String, type: ListType, circleId: Int?, strict: Boolean,
    ) = apiCall {
        api.updateList(listId, ListRequestDto(name.trim(), type.apiValue, circleId, strict))
        refresh().getOrThrow()
    }

    override suspend fun deleteList(listId: Int) = apiCall {
        api.deleteList(listId)
        listDao.deleteCompletely(listId)
    }

    override suspend fun createShare(listId: Int, permission: SharePermission) = apiCall {
        val response = api.shareList(listId, ShareRequestDto(permission.apiValue))
        val entity = response.listShare.toEntity()
        shareDao.upsert(entity)
        CreatedShare(entity, response.shareLink)
    }
}