var searchTimeout,
    previousValue = '';

function searchSubmit() {
    OnAjaxFormButtonClick("secrets-form")
}

function searchSuccess() {
    $('#secrets-keyword').removeClass('loading');
}

function sortSecretSuccess() {
    $("#sort-secrets-loading").hide(),
        $("#sort-secrets-list").show()
}


(function () {
    $("#secretsSortOrderOptions").change(function () {
        $("#secret-sort-order-hidden").val($(this).val());
        $('#secrets-sort-form').submit();

        $("#sort-secrets-list").hide();
        $("#sort-secrets-loading").show();
    }),

        $("input:radio[name=SecretsTypeFilter]").click(function () {
            $("#secret-type-filter-hidden").val($(this).val());
            $('#secrets-filter-form').submit();
            $("#sort-secrets-list").hide();
            $("#sort-secrets-loading").show();
        }),

        $("#sharedSecretsLimitOptions").change(function () {
            $('#secrets-filter-form').submit();
            $("#sort-secrets-list").hide();
            $("#sort-secrets-loading").show();
        })
});

(function () {
    $('#secrets-keyword').keyup(function (e) {
        if ($(this).val() != previousValue || e.keyCode != 13) {
            previousValue = $(this).val();
            $(this).addClass('loading');
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(searchSubmit, 300);
        }
    });
});
