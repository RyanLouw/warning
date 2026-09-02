function filterTable(inputId, tableId, textCellSelector) {
    const input = document.getElementById(inputId);
    const table = document.getElementById(tableId);
    if (!input || !table) return;

    input.addEventListener("keyup", function () {
        const filter = input.value.toLowerCase();
        const rows = table.querySelectorAll("tbody tr");

        rows.forEach(row => {
            const cell = row.querySelector(textCellSelector);
            const text = (cell?.innerText || "").toLowerCase();
            row.style.display = text.includes(filter) ? "" : "none";
        });
    });
}

filterTable("questionSearch", "questionsTable", ".q-text");
filterTable("categorySearch", "categoriesTable", ".c-text");

//const modalEditQuestion = document.getElementById('modalEditQuestion');
//if (modalEditQuestion) {
//    modalEditQuestion.addEventListener('show.bs.modal', function (event) {
//        const btn = event.relatedTarget;
//        if (!btn) return;

//        document.getElementById('editQuestionId').value = btn.getAttribute('data-id') || '';
//        document.getElementById('editQuestionText').value = btn.getAttribute('data-text') || '';
//        document.getElementById('editQuestionControl').value = btn.getAttribute('data-control') || '';
//        document.getElementById('editQuestionConfig').value = btn.getAttribute('data-config') || '';

//        const active = (btn.getAttribute('data-active') || '').toLowerCase() === 'true';
//        document.getElementById('editQuestionActive').checked = active;
//    });
//}

const modalEditCategory = document.getElementById('modalEditCategory');
if (modalEditCategory) {
    modalEditCategory.addEventListener('show.bs.modal', function (event) {
        const btn = event.relatedTarget;
        if (!btn) return;

        document.getElementById('editCategoryId').value = btn.getAttribute('data-id') || '';
        document.getElementById('editCategoryName').value = btn.getAttribute('data-name') || '';

        const active = (btn.getAttribute('data-active') || '').toLowerCase() === 'true';
        document.getElementById('editCategoryActive').checked = active;
    });
}

const modalManage = document.getElementById('modalManageCategoryQuestions');
if (modalManage) {
    modalManage.addEventListener('show.bs.modal', function (event) {
        const btn = event.relatedTarget;
        if (!btn) return;

        const categoryId = btn.getAttribute('data-id') || '';
        const categoryName = btn.getAttribute('data-name') || '';

        const titleEl = document.getElementById('manageCategoryTitle');
        if (titleEl) titleEl.innerText = `Manage Questions: ${categoryName}`;

        const hiddenId = document.getElementById('manageCategoryId');
        if (hiddenId) hiddenId.value = categoryId;
    });
}