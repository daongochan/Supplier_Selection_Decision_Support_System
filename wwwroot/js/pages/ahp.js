/**
 * AHP Matrix Interactive Calculations & Fraction Formatting
 */
(function () {
    "use strict";

    function formatAHPFraction(val) {
        if (val <= 0 || isNaN(val)) return "—";
        const rounded = Math.round(val * 10000) / 10000;
        if (rounded === 1) return "1";
        if (Math.abs(rounded - 0.5) < 0.001) return "1/2";
        if (Math.abs(rounded - 0.3333) < 0.001) return "1/3";
        if (Math.abs(rounded - 0.25) < 0.001) return "1/4";
        if (Math.abs(rounded - 0.2) < 0.001) return "1/5";
        if (Math.abs(rounded - 0.1667) < 0.001) return "1/6";
        if (Math.abs(rounded - 0.1429) < 0.001) return "1/7";
        if (Math.abs(rounded - 0.125) < 0.001) return "1/8";
        if (Math.abs(rounded - 0.1111) < 0.001) return "1/9";
        if (rounded >= 2 && rounded <= 9 && Number.isInteger(rounded)) return rounded.toString();
        return rounded.toFixed(4).replace(/\.?0+$/, "");
    }

    document.addEventListener("DOMContentLoaded", function () {
        const inputs = document.querySelectorAll(".ahp-input");
        inputs.forEach(function (inp) {
            inp.addEventListener("input", function () {
                const row = inp.getAttribute("data-row-id");
                const col = inp.getAttribute("data-col-id");
                if (!row || !col) return;

                const invCell = document.querySelector('[data-source-row="' + row + '"][data-source-col="' + col + '"]');
                if (!invCell) return;

                const val = parseFloat(inp.value);
                if (!isNaN(val) && val > 0) {
                    const invVal = 1.0 / val;
                    invCell.textContent = formatAHPFraction(invVal);
                    invCell.className = "ahp-inverse-value";
                    invCell.setAttribute("aria-label", "Giá trị nghịch đảo đã tính: " + formatAHPFraction(invVal));
                    invCell.style.background = "var(--lavender-soft)";
                    invCell.style.color = "var(--lavender-dark)";
                    setTimeout(function () {
                        invCell.style.background = "";
                        invCell.style.color = "";
                    }, 500);
                } else {
                    invCell.textContent = "—";
                    invCell.className = "ahp-empty";
                    invCell.setAttribute("aria-label", "Chưa nhập so sánh nghịch đảo");
                }
            });
        });
    });
})();
