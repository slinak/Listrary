package com.listrary.android

import android.os.Bundle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import com.listrary.android.ui.navigation.ListraryNavHost
import com.listrary.android.ui.theme.ListraryTheme
import dagger.hilt.android.AndroidEntryPoint

// AppCompatActivity (not ComponentActivity) so AppCompatDelegate.setApplicationLocales works.
@AndroidEntryPoint
class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            ListraryTheme {
                ListraryNavHost()
            }
        }
    }
}