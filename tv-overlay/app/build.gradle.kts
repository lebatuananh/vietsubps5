plugins {
    id("com.android.application")
}

android {
    namespace = "vn.vietsub.ps5.tv"
    compileSdk = 35

    defaultConfig {
        applicationId = "vn.vietsub.ps5.tv"
        minSdk = 29
        targetSdk = 35
        versionCode = 1
        versionName = "0.1.0-phase1"
    }

    buildFeatures {
        buildConfig = true // BuildConfig.DEBUG: extra am start chỉ có ở bản debug
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    testOptions {
        unitTests.all { it.useJUnit() }
    }
}

dependencies {
    implementation("androidx.core:core-ktx:1.16.0")        // ServiceCompat.startForeground (minSdk 29 < API 34)
    implementation("androidx.window:window:1.4.0")          // WindowMetricsCalculator (WindowMetrics chỉ từ API 30)
    testImplementation("junit:junit:4.13.2")
}
