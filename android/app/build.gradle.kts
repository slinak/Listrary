plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.serialization)
    alias(libs.plugins.ksp)
    alias(libs.plugins.hilt)
}

fun prop(name: String): String = providers.gradleProperty(name).get()

android {
    namespace = "com.listrary.android"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.listrary.android"
        minSdk = 26
        targetSdk = 34
        versionCode = 1
        versionName = "1.0.0"

        buildConfigField("String", "TENANT_ID", "\"${prop("listrary.tenantId")}\"")
        buildConfigField("String", "ANDROID_CLIENT_ID", "\"${prop("listrary.androidClientId")}\"")
        buildConfigField("String", "API_SCOPE", "\"${prop("listrary.apiScope")}\"")
        buildConfigField("String", "SIGNATURE_HASH", "\"${prop("listrary.signatureHash")}\"")
        buildConfigField("String", "DEEP_LINK_HOST", "\"${prop("listrary.deepLinkHost")}\"")
        manifestPlaceholders["deepLinkHost"] = prop("listrary.deepLinkHost")
        manifestPlaceholders["signatureHash"] = prop("listrary.signatureHash")
    }

    buildTypes {
        debug {
            buildConfigField("String", "BASE_URL", "\"${prop("listrary.debugBaseUrl")}\"")
        }
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
            buildConfigField("String", "BASE_URL", "\"${prop("listrary.releaseBaseUrl")}\"")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }
    kotlinOptions { jvmTarget = "17" }
    buildFeatures {
        compose = true
        buildConfig = true
    }
    composeOptions { kotlinCompilerExtensionVersion = libs.versions.composeCompiler.get() }
    sourceSets["main"].java.srcDir("src/main/kotlin")
    packaging { resources.excludes += "/META-INF/{AL2.0,LGPL2.1}" }
}

ksp { arg("room.schemaLocation", "$projectDir/schemas") }

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.lifecycle.runtime.compose)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.compose.material3)
    implementation(libs.androidx.compose.material.icons.extended)
    debugImplementation(libs.androidx.compose.ui.tooling)
    implementation(libs.androidx.navigation.compose)
    implementation(libs.androidx.hilt.navigation.compose)
    implementation(libs.hilt.android)
    ksp(libs.hilt.compiler)
    implementation(libs.androidx.room.runtime)
    implementation(libs.androidx.room.ktx)
    ksp(libs.androidx.room.compiler)
    implementation(libs.retrofit)
    implementation(libs.retrofit.serialization)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging)
    implementation(libs.kotlinx.serialization.json)
    implementation(libs.kotlinx.coroutines.android)
    implementation(libs.msal) {
        exclude(group = "io.opentelemetry")
    }
}
