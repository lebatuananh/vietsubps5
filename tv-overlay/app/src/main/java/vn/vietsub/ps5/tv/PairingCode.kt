package vn.vietsub.ps5.tv

import java.security.SecureRandom
import java.util.Random

object PairingCode {
    /** Bảng chữ không nhập nhằng (22 ký tự). */
    const val ALPHABET = "ACDEFHJKMNPRTUVWXY3479"

    /** 8 ký tự ngẫu nhiên thuộc [ALPHABET] (chưa có dấu '-'). */
    fun generate(random: Random = SecureRandom()): String =
        buildString { repeat(8) { append(ALPHABET[random.nextInt(ALPHABET.length)]) } }

    /** "ABCDEFGH" → "ABCD-EFGH". */
    fun format(raw: String): String {
        val n = normalize(raw)
        return if (n.length == 8) n.substring(0, 4) + "-" + n.substring(4) else n
    }

    /** Hoa, bỏ '-' và khoảng trắng. */
    fun normalize(s: String): String = s.filter { it != '-' && !it.isWhitespace() }.uppercase()

    fun isValid(s: String): Boolean = normalize(s).let { it.length == 8 && it.all { c -> c in ALPHABET } }

    /** So mã không phân biệt hoa/thường, bỏ '-'. */
    fun matches(a: String, b: String): Boolean = normalize(a) == normalize(b)
}
