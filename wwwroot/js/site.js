document.addEventListener("DOMContentLoaded", function () {

    const sidebar = document.querySelector(".app-sidebar");
    const sidebarToggle = document.querySelector("#sidebarToggle");

    if (!sidebar || !sidebarToggle) {
        return;
    }

    sidebarToggle.addEventListener("click", function () {

        sidebar.classList.toggle("open");

        const isOpen =
            sidebar.classList.contains("open");

        sidebarToggle.setAttribute(
            "aria-expanded",
            isOpen.toString()
        );

    });


    // Close sidebar after clicking a navigation item
    // on smaller screens.

    const sidebarLinks =
        sidebar.querySelectorAll(".sidebar-link");

    sidebarLinks.forEach(function (link) {

        link.addEventListener("click", function () {

            if (window.innerWidth <= 900) {

                sidebar.classList.remove("open");

                sidebarToggle.setAttribute(
                    "aria-expanded",
                    "false"
                );
            }

        });

    });

});