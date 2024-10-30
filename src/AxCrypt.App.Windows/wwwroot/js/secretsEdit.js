(function () {
    $('#secret-edit').click(function () {
        $('#secret-edit-form').submit();
    });
    $('#secret-suggest-password').click(function () {
        $('#secret-suggest-form').submit();
    });
});

var secretSuggestionSuccess = function (data) {
    $('#pwd-secret-inp').removeClass('loading');
    $('#secret-suggest-password').removeAttr('disabled');
    if (data && data.length)
        $('#pwd-secret-inp').val(data[0]);
}

var secretSuggestionBegin = function () {
    $('#pwd-secret-inp').addClass('loading');
    $('#secret-suggest-password').attr('disabled', 'true');
}