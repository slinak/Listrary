package com.listrary.android.di

import com.listrary.android.data.repository.CirclesRepository
import com.listrary.android.data.repository.CirclesRepositoryImpl
import com.listrary.android.data.repository.ListItemsRepository
import com.listrary.android.data.repository.ListItemsRepositoryImpl
import com.listrary.android.data.repository.ListsRepository
import com.listrary.android.data.repository.ListsRepositoryImpl
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
abstract class RepositoryModule {
    @Binds @Singleton
    abstract fun bindListsRepository(impl: ListsRepositoryImpl): ListsRepository

    @Binds @Singleton
    abstract fun bindListItemsRepository(impl: ListItemsRepositoryImpl): ListItemsRepository

    @Binds @Singleton
    abstract fun bindCirclesRepository(impl: CirclesRepositoryImpl): CirclesRepository
}