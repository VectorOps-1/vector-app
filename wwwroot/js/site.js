// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    document.addEventListener("DOMContentLoaded", function () {
        const taskNotification = document.getElementById("taskNotification");
        const taskCount = document.getElementById("taskCount");

        if (!taskNotification || !taskCount) {
            return;
        }

        const role = sessionStorage.getItem("vectorAccessRole");
        if (!role || role === "senior") {
            return;
        }

        const userId = role === "manager" ? 2 : 1;

        fetch(`/TaskNotificationCount?userId=${userId}`, { cache: "no-store" })
            .then(response => response.ok ? response.json() : { count: 0 })
            .then(data => {
                const count = Number(data.count || 0);
                if (count > 0) {
                    taskCount.textContent = String(count);
                    taskNotification.classList.add("visible");
                }
            })
            .catch(() => {
                // Keep the UI quiet if the count endpoint is unavailable.
            });
    });
})();
