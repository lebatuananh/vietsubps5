// AGP 9.0.1 có built-in Kotlin (KGP 2.2.10 là phụ thuộc runtime của AGP) — KHÔNG apply org.jetbrains.kotlin.android.
plugins {
    id("com.android.application") version "9.0.1" apply false
}
