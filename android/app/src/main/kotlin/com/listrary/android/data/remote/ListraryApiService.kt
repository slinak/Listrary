package com.listrary.android.data.remote

import com.listrary.android.data.remote.dto.*
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

/** Retrofit definition of the Listrary REST API (base path /api/v1/). */
interface ListraryApiService {

    // Lists
    @GET("lists")
    suspend fun getLists(): ListsResponseDto

    @GET("lists/{id}")
    suspend fun getList(@Path("id") id: Int, @Query("shareToken") shareToken: String? = null): ListDetailDto

    @POST("lists")
    suspend fun createList(@Body body: ListRequestDto): ListSummaryDto

    @PUT("lists/{id}")
    suspend fun updateList(@Path("id") id: Int, @Body body: ListRequestDto): ListSummaryDto

    @DELETE("lists/{id}")
    suspend fun deleteList(@Path("id") id: Int)

    @POST("lists/{id}/share")
    suspend fun shareList(@Path("id") id: Int, @Body body: ShareRequestDto): ShareResponseDto

    // Items
    @POST("lists/{listId}/items")
    suspend fun addItem(
        @Path("listId") listId: Int,
        @Body body: ItemRequestDto,
        @Query("shareToken") shareToken: String? = null,
    ): ItemResponseDto

    @PUT("lists/{listId}/items/{itemId}")
    suspend fun updateItem(
        @Path("listId") listId: Int,
        @Path("itemId") itemId: Int,
        @Body body: UpdateItemRequestDto,
        @Query("shareToken") shareToken: String? = null,
    ): ItemResponseDto

    @DELETE("lists/{listId}/items/{itemId}")
    suspend fun deleteItem(
        @Path("listId") listId: Int,
        @Path("itemId") itemId: Int,
        @Query("shareToken") shareToken: String? = null,
    )

    @POST("lists/{listId}/items/{itemId}/toggle")
    suspend fun toggleItem(
        @Path("listId") listId: Int,
        @Path("itemId") itemId: Int,
        @Query("shareToken") shareToken: String? = null,
    ): ListItemDto

    @POST("lists/{listId}/items/mark-all-incomplete")
    suspend fun markAllIncomplete(
        @Path("listId") listId: Int,
        @Query("shareToken") shareToken: String? = null,
    )

    @GET("lists/{listId}/items/suggestions/names")
    suspend fun suggestNames(
        @Path("listId") listId: Int,
        @Query("query") query: String,
        @Query("shareToken") shareToken: String? = null,
    ): List<String>

    @GET("lists/{listId}/items/suggestions/locations")
    suspend fun suggestLocations(
        @Path("listId") listId: Int,
        @Query("itemName") itemName: String,
        @Query("query") query: String,
        @Query("shareToken") shareToken: String? = null,
    ): List<String>

    // Circles
    @GET("circles")
    suspend fun getCircles(): CirclesResponseDto

    @POST("circles")
    suspend fun createCircle(@Body body: CreateCircleRequestDto): CircleDto

    @GET("circles/{id}")
    suspend fun getCircle(@Path("id") id: Int): CircleDto

    @POST("circles/{id}/invite")
    suspend fun invite(@Path("id") id: Int, @Body body: InviteRequestDto)

    @DELETE("circles/{id}/members/{userId}")
    suspend fun removeMember(@Path("id") id: Int, @Path("userId") userId: String)

    @POST("circles/{id}/leave")
    suspend fun leaveCircle(@Path("id") id: Int)

    @POST("circles/invitations/{invitationId}/accept")
    suspend fun acceptInvitation(@Path("invitationId") invitationId: Int)

    @POST("circles/invitations/{invitationId}/reject")
    suspend fun rejectInvitation(@Path("invitationId") invitationId: Int)

    @DELETE("circles/invitations/{invitationId}")
    suspend fun cancelInvitation(@Path("invitationId") invitationId: Int)

    @DELETE("circles/{id}")
    suspend fun deleteCircle(@Path("id") id: Int)
}