package com.listrary.android.data.local

import android.content.Context
import androidx.room.Room
import com.listrary.android.data.local.dao.CircleDao
import com.listrary.android.data.local.dao.ListDao
import com.listrary.android.data.local.dao.ListItemDao
import com.listrary.android.data.local.dao.ListShareDao
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object DatabaseModule {

    @Provides
    @Singleton
    fun provideDatabase(@ApplicationContext context: Context): AppDatabase =
        Room.databaseBuilder(context, AppDatabase::class.java, "listrary.db")
            .fallbackToDestructiveMigration()
            .build()

    @Provides fun provideListDao(db: AppDatabase): ListDao = db.listDao()
    @Provides fun provideListItemDao(db: AppDatabase): ListItemDao = db.listItemDao()
    @Provides fun provideListShareDao(db: AppDatabase): ListShareDao = db.listShareDao()
    @Provides fun provideCircleDao(db: AppDatabase): CircleDao = db.circleDao()
}
