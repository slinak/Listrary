package com.listrary.android.data.local

import androidx.room.Database
import androidx.room.RoomDatabase
import com.listrary.android.data.local.dao.CircleDao
import com.listrary.android.data.local.dao.ListDao
import com.listrary.android.data.local.dao.ListItemDao
import com.listrary.android.data.local.dao.ListShareDao
import com.listrary.android.data.local.entities.CircleEntity
import com.listrary.android.data.local.entities.CircleInvitationEntity
import com.listrary.android.data.local.entities.CircleMemberEntity
import com.listrary.android.data.local.entities.ListEntity
import com.listrary.android.data.local.entities.ListItemEntity
import com.listrary.android.data.local.entities.ListShareEntity

@Database(
    entities = [
        ListEntity::class,
        ListItemEntity::class,
        ListShareEntity::class,
        CircleEntity::class,
        CircleMemberEntity::class,
        CircleInvitationEntity::class,
    ],
    version = 1,
    exportSchema = false,
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun listDao(): ListDao
    abstract fun listItemDao(): ListItemDao
    abstract fun listShareDao(): ListShareDao
    abstract fun circleDao(): CircleDao
}