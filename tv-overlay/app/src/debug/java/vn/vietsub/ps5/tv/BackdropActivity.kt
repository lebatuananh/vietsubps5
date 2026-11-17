package vn.vietsub.ps5.tv

import android.app.Activity
import android.graphics.BitmapFactory
import android.os.Bundle
import android.view.View
import android.view.WindowManager
import android.widget.ImageView
import java.io.File

/**
 * D15 (CHỈ debug): hiển thị toàn màn một PNG thay nguồn HDMI trên emulator — lớp phủ vẽ đè lên nó.
 * `am start -n vn.vietsub.ps5.tv/.BackdropActivity --es backdrop /sdcard/Download/x.png`
 * Không đọc được đường dẫn đó (scoped storage) ⇒ thử `filesDir/<tên file>` (đẩy bằng `adb shell run-as … cp`).
 */
class BackdropActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        val iv = ImageView(this).apply { scaleType = ImageView.ScaleType.FIT_XY; setBackgroundColor(0xFF000000.toInt()) }
        setContentView(iv)
        load(intent.getStringExtra("backdrop"), iv)
    }

    override fun onNewIntent(intent: android.content.Intent) {
        super.onNewIntent(intent)
        load(intent.getStringExtra("backdrop"), findViewById<View>(android.R.id.content).let { (it as android.view.ViewGroup).getChildAt(0) as ImageView })
    }

    private fun load(path: String?, iv: ImageView) {
        if (path == null) return
        val candidates = listOf(File(path), File(filesDir, File(path).name))
        val f = candidates.firstOrNull { it.canRead() }
        if (f == null) { android.util.Log.w("VietSubBackdrop", "không đọc được: $path"); return }
        iv.setImageBitmap(BitmapFactory.decodeFile(f.absolutePath))
        android.util.Log.i("VietSubBackdrop", "hiển thị ${f.absolutePath}")
    }
}
