/**
 * AHP Matrix Interactive Pairwise Scale Selection & Reciprocal Calculations
 * Canonical Saaty scale: 1/9, 1/8, 1/7, 1/6, 1/5, 1/4, 1/3, 1/2, 1, 2, 3, 4, 5, 6, 7, 8, 9
 */
(function () {
    "use strict";

    const RECIPROCAL_MAP = {
        "1/9": "9",
        "1/8": "8",
        "1/7": "7",
        "1/6": "6",
        "1/5": "5",
        "1/4": "4",
        "1/3": "3",
        "1/2": "2",
        "1": "1",
        "2": "1/2",
        "3": "1/3",
        "4": "1/4",
        "5": "1/5",
        "6": "1/6",
        "7": "1/7",
        "8": "1/8",
        "9": "1/9"
    };

    const DECIMAL_MAP = {
        "1/9": 0.1111,
        "1/8": 0.1250,
        "1/7": 0.1429,
        "1/6": 0.1667,
        "1/5": 0.2000,
        "1/4": 0.2500,
        "1/3": 0.3333,
        "1/2": 0.5000,
        "1": 1.0000,
        "2": 2.0000,
        "3": 3.0000,
        "4": 4.0000,
        "5": 5.0000,
        "6": 6.0000,
        "7": 7.0000,
        "8": 8.0000,
        "9": 9.0000
    };

    const INTENSITY_MAP = {
        "1": "equally",
        "2": "equally to moderately",
        "3": "moderately",
        "4": "moderately to strongly",
        "5": "strongly",
        "6": "strongly to very strongly",
        "7": "very strongly",
        "8": "very strongly to extremely",
        "9": "extremely"
    };

    function getComparisonExplanation(rowName, colName, code) {
        if (!code || !RECIPROCAL_MAP[code]) return "";
        if (code === "1") {
            return "'" + rowName + "' and '" + colName + "' are equally important (1)";
        }
        if (DECIMAL_MAP[code] > 1) {
            const intensity = INTENSITY_MAP[code] || "more";
            return "'" + rowName + "' is " + intensity + " more important than '" + colName + "' (" + code + ")";
        } else {
            const reciprocalCode = RECIPROCAL_MAP[code];
            const intensity = INTENSITY_MAP[reciprocalCode] || "more";
            return "'" + colName + "' is " + intensity + " more important than '" + rowName + "' (" + reciprocalCode + ")";
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        const selects = document.querySelectorAll(".ahp-select");

        selects.forEach(function (select) {
            select.addEventListener("change", function () {
                const row = select.getAttribute("data-row-id");
                const col = select.getAttribute("data-col-id");
                const rowName = select.getAttribute("data-row-name") || "Criterion A";
                const colName = select.getAttribute("data-col-name") || "Criterion B";
                const selectedCode = select.value.trim();

                // Update contextual explanation
                const expEl = document.querySelector('[data-explanation-row="' + row + '"][data-explanation-col="' + col + '"]');
                if (expEl) {
                    expEl.textContent = getComparisonExplanation(rowName, colName, selectedCode);
                }

                // Update hidden decimal input
                const hiddenInput = document.querySelector('.ahp-hidden-decimal[data-row-id="' + row + '"][data-col-id="' + col + '"]');
                if (hiddenInput) {
                    hiddenInput.value = DECIMAL_MAP[selectedCode] ? DECIMAL_MAP[selectedCode].toFixed(4) : "";
                }

                // Update lower-triangle symmetric cell
                const invCell = document.querySelector('[data-source-row="' + row + '"][data-source-col="' + col + '"]');
                if (!invCell) return;

                if (selectedCode && RECIPROCAL_MAP[selectedCode]) {
                    const reciprocalCode = RECIPROCAL_MAP[selectedCode];
                    invCell.textContent = reciprocalCode;
                    invCell.className = "ahp-inverse-value";
                    invCell.setAttribute("aria-label", "Reciprocal value: " + reciprocalCode);
                    invCell.style.transition = "background-color 0.25s ease, color 0.25s ease";
                    invCell.style.background = "var(--lavender-soft, #ede9fe)";
                    invCell.style.color = "var(--lavender-dark, #5b21b6)";
                    setTimeout(function () {
                        invCell.style.background = "";
                        invCell.style.color = "";
                    }, 500);
                } else {
                    invCell.textContent = "—";
                    invCell.className = "ahp-empty";
                    invCell.setAttribute("aria-label", "No reciprocal value yet");
                }
            });
        });
    });
})();
