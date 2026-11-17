package vn.vietsub.ps5.tv

import java.util.Random
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class PairingCodeTest {
    @Test fun alphabet_is_the_22_unambiguous_chars() {
        assertEquals("ACDEFHJKMNPRTUVWXY3479", PairingCode.ALPHABET)
        assertEquals(22, PairingCode.ALPHABET.length)
        assertTrue(PairingCode.ALPHABET.none { it in "01258BGILOQSZ" })
    }

    @Test fun generate_gives_eight_chars_from_alphabet() {
        val rnd = Random(42)
        repeat(200) {
            val c = PairingCode.generate(rnd)
            assertEquals(8, c.length)
            assertTrue(c, c.all { it in PairingCode.ALPHABET })
        }
    }

    @Test fun generate_is_not_constant_and_uses_the_whole_alphabet() {
        val rnd = Random(7)
        val used = HashSet<Char>()
        val codes = HashSet<String>()
        repeat(500) { val c = PairingCode.generate(rnd); codes.add(c); used.addAll(c.toList()) }
        assertTrue(codes.size > 450)
        assertEquals(PairingCode.ALPHABET.toSet(), used)
    }

    @Test fun format_groups_four_dash_four() = assertEquals("ACDE-FHJK", PairingCode.format("ACDEFHJK"))

    @Test fun normalize_uppercases_and_strips_dash_and_spaces() {
        assertEquals("ACDEFHJK", PairingCode.normalize("acde-fhjk"))
        assertEquals("ACDEFHJK", PairingCode.normalize(" ACDE FHJK "))
    }

    @Test fun matches_ignores_case_and_dash() {
        assertTrue(PairingCode.matches("acde-fhjk", "ACDEFHJK"))
        assertTrue(PairingCode.matches("ACDE-FHJK", "acdefhjk"))
        assertFalse(PairingCode.matches("ACDE-FHJK", "ACDE-FHJM"))
    }

    @Test fun isValid() {
        assertTrue(PairingCode.isValid("ACDE-FHJK"))
        assertFalse(PairingCode.isValid("ACDE-FHJ"))
        assertFalse(PairingCode.isValid("ACDE-FHJ0"))
        assertFalse(PairingCode.isValid("ACDE-FHJI"))
    }
}
