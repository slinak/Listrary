package com.listrary.android.ui.navigation

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.ShoppingCart
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.stringResource
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import androidx.navigation.navDeepLink
import com.listrary.android.R
import com.listrary.android.auth.AuthState
import com.listrary.android.ui.auth.SignInScreen
import com.listrary.android.ui.auth.SignInViewModel
import com.listrary.android.ui.circles.CircleDetailScreen
import com.listrary.android.ui.circles.CirclesScreen
import com.listrary.android.ui.common.LoadingState
import com.listrary.android.ui.listdetail.ListDetailScreen
import com.listrary.android.ui.lists.ListsScreen
import com.listrary.android.ui.settings.SettingsScreen

private data class TopLevelItem(val route: String, val labelRes: Int, val icon: ImageVector)

private val topLevelItems = listOf(
    TopLevelItem(Destinations.LISTS, R.string.nav_lists, Icons.Filled.ShoppingCart),
    TopLevelItem(Destinations.CIRCLES, R.string.nav_circles, Icons.Filled.Groups),
    TopLevelItem(Destinations.SETTINGS, R.string.nav_settings, Icons.Filled.Settings),
)

@Composable
fun ListraryNavHost(authViewModel: SignInViewModel = hiltViewModel()) {
    val authState by authViewModel.authState.collectAsStateWithLifecycle()
    if (authState is AuthState.Loading) {
        LoadingState()
        return
    }

    val navController = rememberNavController()
    val backStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = backStackEntry?.destination?.route
    val startDestination = remember { if (authState is AuthState.SignedIn) Destinations.LISTS else Destinations.SIGN_IN }

    AuthRedirects(navController, authState)

    Scaffold(
        bottomBar = {
            if (currentRoute in Destinations.topLevel) {
                NavigationBar {
                    topLevelItems.forEach { item ->
                        NavigationBarItem(
                            selected = currentRoute == item.route,
                            onClick = {
                                navController.navigate(item.route) {
                                    popUpTo(Destinations.LISTS) { saveState = true }
                                    launchSingleTop = true
                                    restoreState = true
                                }
                            },
                            icon = { Icon(item.icon, contentDescription = null) },
                            label = { Text(stringResource(item.labelRes)) },
                        )
                    }
                }
            }
        },
    ) { innerPadding ->
        NavHost(
            navController = navController,
            startDestination = startDestination,
            modifier = Modifier.padding(bottom = innerPadding.calculateBottomPadding()),
        ) {
            composable(Destinations.SIGN_IN) { SignInScreen() }
            composable(Destinations.LISTS) {
                ListsScreen(onOpenList = { navController.navigate(Destinations.listDetail(it)) })
            }
            composable(Destinations.CIRCLES) {
                CirclesScreen(onOpenCircle = { navController.navigate(Destinations.circleDetail(it)) })
            }
            composable(Destinations.SETTINGS) { SettingsScreen() }
            composable(
                route = Destinations.LIST_DETAIL,
                arguments = listOf(
                    navArgument(Destinations.ARG_LIST_ID) { type = NavType.IntType },
                    navArgument(Destinations.ARG_SHARE_TOKEN) {
                        type = NavType.StringType
                        nullable = true
                        defaultValue = null
                    },
                ),
                deepLinks = listOf(
                    navDeepLink { uriPattern = Destinations.listAppLink },
                    navDeepLink { uriPattern = Destinations.LIST_CUSTOM_SCHEME_LINK },
                ),
            ) {
                ListDetailScreen(onBack = { navController.popBackStack() })
            }
            composable(
                route = Destinations.CIRCLE_DETAIL,
                arguments = listOf(navArgument(Destinations.ARG_CIRCLE_ID) { type = NavType.IntType }),
            ) {
                CircleDetailScreen(onBack = { navController.popBackStack() })
            }
        }
    }
}

/**
 * Sends signed-out users to the sign-in screen (except when viewing a list through a share
 * token, which works anonymously) and returns signed-in users from it to Lists.
 */
@Composable
private fun AuthRedirects(navController: NavHostController, authState: AuthState) {
    LaunchedEffect(authState) {
        val entry = navController.currentBackStackEntry
        val route = entry?.destination?.route
        when (authState) {
            is AuthState.SignedOut -> {
                val viewingSharedList = route == Destinations.LIST_DETAIL &&
                    !entry?.arguments?.getString(Destinations.ARG_SHARE_TOKEN).isNullOrBlank()
                if (route != Destinations.SIGN_IN && !viewingSharedList) {
                    navController.navigate(Destinations.SIGN_IN) { popUpTo(0) { inclusive = true } }
                }
            }
            is AuthState.SignedIn -> if (route == Destinations.SIGN_IN) {
                navController.navigate(Destinations.LISTS) { popUpTo(Destinations.SIGN_IN) { inclusive = true } }
            }
            AuthState.Loading -> Unit
        }
    }
}