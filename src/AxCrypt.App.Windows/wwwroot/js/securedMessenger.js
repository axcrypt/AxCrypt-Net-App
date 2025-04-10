window.SemScript = function () {
    //active button text
    window.updateButtonText = () => {
        let buttonText;
        if ($('#inbox-tab').hasClass('active')) {
            buttonText = 'Inbox';
        } else if ($('#sent-tab').hasClass('active')) {
            buttonText = 'Sent';
        } else if ($('#unread-tab').hasClass('active')) {
            buttonText = 'Unread';
        }
        $('.mutli-slct-btn').text(buttonText);
    }
    $(".sub-head-link").on("click", function () {
        $(".sub-head-link").removeClass("active");
        $(this).addClass("active");
        updateButtonText();
        $("#new-message-section").hide();
    });

    $('.view-section').hide();
    $("#new-message-section").hide();

    $("#viewer-messenger-section").hide();
    $("#messenger-loading-section").hide();

    $(document).ready(function () {
        if ($('.unread-text').hasClass('active')) {
            $('.inbox-section').addClass('read-txt-size');
        } else {
            $('.inbox-section').removeClass('read-txt-size');
        }
    });

    var emptyId = "";
    $(".unread-message").on("click", function (event) {
        if ($(event.target).is(':checkbox')) {
            return;
        }

        $("#new-message-section").hide();
        $("#mb-new-message").hide();

        var messageId = $(this).data("id");
        var parentId = $(this).data("parent");
        if (parentId == emptyId) {
            $("#selected-message-id-hidden").val(messageId);
        }

        if (parentId != emptyId) {
            $("#selected-message-id-hidden").val(parentId);
        }

        $(".empty-sec").hide();
        $("#messenger-loading-section").show();

        ToggleMessageActive(parentId != emptyId ? parentId : messageId);

        OnMessageSelected(messageId);
    });

    function ToggleMessageActive(selectedMsgId) {
        $(".unread-message").each(function () {
            if ($(this).find("table.active").length > 0) {
                $(this).find("table").removeClass('active');
            }
        });

        $("#unread-message-" + selectedMsgId + " table").addClass("active");
    }

    function OnMessageSelected(messageId) {
        $(".view-section").show();

        if (window.innerWidth <= 979) {
            $("#sec-msg-search-section").hide();
        }

        $("#viewer-messenger-section").show();

        var hiddenInput = $("#selected-message-id-hidden");
        if (hiddenInput.length === 0) {
            console.error("Element with ID 'selected-message-id-hidden' not found.");
            return;
        }

        hiddenInput.val(messageId);
        var msgrId = hiddenInput.val();

        console.log("Selected Message ID:", msgrId);

        $("#unread-message-" + msgrId).removeClass("read-text");
        $("#unread-message-" + msgrId).find(".mob-hide").hide();

        if (window.dotNetHelperView) {
            window.dotNetHelperView.invokeMethodAsync('FetchMessageDetails', msgrId)
                .then(() => {
                    $("#messenger-loading-section").hide();
                })
                .catch(error => {
                    console.error("Error in FetchMessageDetails:", error);
                });
        } else {
            console.error("dotNetHelper is not initialized.");
        }
    }

    $("#reply-icon2").click(function () {
        $("#reply-message").show();
        $("#reply-icon2").hide();
    });

    $("#new-message").hide();
    $("#create-new-message").click(function () {
        $("#new-message-section").show();
        $(".empty-sec").hide();
        $('.view-section').hide();
        $("#reply-message").hide();
    });

    $("#dropdownButton1").click(function () {
        $('#dropdownMenu1').toggle();
    });

    $("#dropdownForm1 input[type='radio']").change(function () {
        var selectedValue = $(this).val();
        $('#dropdownButton1').text(selectedValue);
        $('#dropdownMenu1').hide();
        $('#dropdownForm1 label').removeClass('active');
        $(this).closest('label').addClass('active');
    });

    $(document).mouseup(function (e) {
        var container = $("#dropdownMenu1");
        if (!container.is(e.target) && container.has(e.target).length === 0 && !$("#dropdownButton1").is(e.target)) {
            container.hide();
        }
    });

    $("#dropdownButton2").click(function () {
        $('#dropdownMenu2').toggle();
    });

    $("#dropdownForm2 input[type='radio']").change(function () {
        var selectedValue = $(this).val();
        $('#dropdownButton2').text(selectedValue);
        $('#dropdownMenu2').hide();
        $('#dropdownForm2 label').removeClass('active');
        $(this).closest('label').addClass('active');
    });

    $(document).mouseup(function (e) {
        var container = $("#dropdownMenu2");
        if (!container.is(e.target) && container.has(e.target).length === 0 && !$("#dropdownButton2").is(e.target)) {
            container.hide();
        }
    });

    $(".clse-img").click(function () {
        $('.alert-box').hide();
    });

    document.getElementById('secured-mess-body').addEventListener('input', function () {
        var messageBody = document.getElementById('secured-mess-body');
        var sendImage = document.querySelector('.snd-mb');

        if (messageBody.value.length > 0) {
            sendImage.classList.add('active');
        } else {
            sendImage.classList.remove('active');
        }
    });

    window.setDotNetHelper = function (dotNetHelper) {
        window.dotNetHelper = dotNetHelper;
    };

    window.setDotNetHelperNew = function (dotNetHelper) {
        window.dotNetHelperNew = dotNetHelper;
    };

    window.setDotNetHelperView = function (dotNetHelper) {
        window.dotNetHelperView = dotNetHelper;
    };

    window.setDotNetHelperLoad = function (dotNetHelper) {
        window.dotNetHelperLoad = dotNetHelper;
    };

    //multi select
    window.multiAction = function (action) {
        const checkedIds = Array.from(document.querySelectorAll('.hover-checkbox'))
            .filter(checkbox => checkbox.checked)
            .map(checkbox => checkbox.getAttribute('data-id'));

        if (checkedIds.length === 0) {
            alert("Please select at least one message.");
            return;
        }

        console.log(`${action} initiated for: ${checkedIds.join(", ")}`);

        if (window.dotNetHelper) {
            window.dotNetHelper.invokeMethodAsync(action, checkedIds);
        } else {
            console.error("dotNetHelper is not initialized.");
        }
    };

    window.OnMessageAction = function (event, messageId, parentId, receivers) {
        var target = event.target;

        if (!messageId) {
            console.error("Message ID is missing.");
            return;
        }

        if (target.name === "delete-button") {
            deleteAction(target, parentId);
            return;
        }

        if (target.name === "reply-button") {
            replyMessage(target, parentId || messageId, messageId, receivers || "");
            return;
        }

        var messageView = $("#secured-messenger-view-" + messageId);
        if (messageView.length && messageView.css('display') !== 'none') {
            messageView.hide();
            return;
        }

        $("#message-id-view-message-hidden").val(messageId);

        window.dotNetHelperView.invokeMethodAsync('ViewMessageById', messageId)
            .then(result => {
                if (result?.message?.theMessage) {
                    UpdateMessageView(result);
                }
            })
            .catch(error => {
                console.error("Error fetching message:", error);
            });
    }

    function UpdateMessageView(result) {
        if (!result || !result.message || !result.message.theMessage) {
            console.error("No valid message found:", result);
            return;
        }

        var selectedMsg = result;
        var textAreaField = "<textarea cols='20' id='secured-mess-body' maxlength='100000' name='encryptedMessage' readonly='readonly' rows='2'>"
            + selectedMsg.message.theMessage +
            "</textarea>";

        $("#secured-messenger-view-" + selectedMsg.id).html(textAreaField);
        $("#secured-messenger-view-" + selectedMsg.id).show();
    }

    window.replyMessage = function (button, parentMsgId, messageId, replyUsers) {
        if (!button || !messageId) {
            console.error("Invalid reply message data.");
            return;
        }

        $("#reply-message").show();
        $(button).hide();

        onClickReplyMessagebtn(parentMsgId, messageId, replyUsers);
    }

    window.onClickReplyMessagebtn = function (parentMsgId, messageId, replyUsers) {
        if (!parentMsgId || !messageId) {
            console.error("Missing Parent ID or Message ID.");
            return;
        }

        $("#parent-message-id-new-message-hidden").val(parentMsgId);
        $("#message-id-new-message-hidden").val(messageId);
        $("#message-reply-users-hidden").val(replyUsers || "");

        $("#new-message-section").show();

        let validParentId = parentMsgId ? parentMsgId.toString() : "";
        let validMessageId = messageId ? messageId.toString() : "";
        let validReplyUsers = replyUsers ? replyUsers.toString() : "";

        if (window.dotNetHelperNew) {
            window.dotNetHelperNew.invokeMethodAsync('UpdateReplyValuesFromJs', validParentId, validMessageId, validReplyUsers)
                .then(() => console.log("C# method invoked successfully"))
                .catch(err => console.error("Error invoking C# method:", err));
        } else {
            console.error("dotNetHelper is not initialized.");
        }
    }

    window.getHiddenFieldValue = function (id) {
        let input = document.getElementById(id);
        return input ? input.value : "";
    };

    function deleteAction(button, parentId) {
        deleteForm(button, parentId);
    }

    function deleteForm(button, parentId) {
        const messageId = button.getAttribute('data-id');
        const hiddenInput = document.getElementById('messenger-id-hidden');
        const hiddenParentId = document.getElementById('messenger-parentid-hidden');
        const form = document.getElementById('message-delete-form');

        if (!hiddenInput || !form) {
            console.error("Form or hidden input is missing!");
            return;
        }

        hiddenInput.value = messageId;
        hiddenParentId.value = parentId;

        if (window.dotNetHelperView) {
            window.dotNetHelperView.invokeMethodAsync('DeleteMessage', messageId, parentId);
        } else {
            console.error("dotNetHelper is not initialized.");
        }
    }
    var pageNumberCount = 0;
    pageNumberCount = parseInt($("#pagenumber-id-hidden").val()) || 0;

    document.addEventListener("DOMContentLoaded", function () {
        window.EnableLoadMore();
    });

    window.EnableLoadMore = function () {
        pageNumberCount++;
        $("#pagenumber-id-hidden").val(pageNumberCount);

        if (window.dotNetHelperLoad) {
            window.dotNetHelperLoad.invokeMethodAsync('EnableLoadMoreAction', pageNumberCount)
                .then(hasMoreMessages => {
                    if (!hasMoreMessages) {
                        $('#load-more-btn').hide();
                    }
                })
                .catch(error => {
                    console.error("Error in EnableLoadMoreAction:", error);
                });
        } else {
            console.error("dotNetHelper is not initialized.");
        }
    };

    window.RestoreLoadMoreButton = function () {
        $("#load-more-btn").show();
        $(".empty-sec").show();
        $("#viewer-messenger-section").hide();
    };

    //checkbox
    document.addEventListener('DOMContentLoaded', function () {
        const checkboxes = document.querySelectorAll('.hover-checkbox');
        const tables = document.querySelectorAll('.hover-table');

        checkboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function () {
                if (this.checked) {
                    tables.forEach(table => table.classList.add('show-all'));
                } else {
                    const anyChecked = Array.from(checkboxes).some(cb => cb.checked);
                    if (!anyChecked) {
                        tables.forEach(table => table.classList.remove('show-all'));
                    }
                }
            });
        });
    });

    var mbNewMessage = "";

    $(document).ready(function () {
        const updateTableBackground = () => {
            let anyChecked = $('.hover-checkbox:checked').length > 0;

            $('.hover-checkbox').each(function () {
                if ($(this).is(':checked')) {
                    $(this).closest('.hover-table').addClass('checked');
                } else {
                    $(this).closest('.hover-table').removeClass('checked');
                }
            });

            if (anyChecked) {
                $('.acc-prfl').hide();
                $('.hover-checkbox').show();
                $('.multi-chckbx').show();
            } else {
                $('.acc-prfl').show();
                $('.hover-checkbox').hide();
                $('.multi-chckbx').hide();
            }

            $('.slct-actions').toggleClass('checkbox-checked', anyChecked);
        };

        $('.multi-chckbx').hide();
        $('.select-option').click(function () {
            $('.acc-prfl').toggle();
            $('.hover-checkbox').toggle();
            $('.multi-chckbx').toggle();
        })

        $('.multi-chckbx').click(function () {
            const checkboxes = $('.hover-checkbox');
            const checkedCount = checkboxes.filter(':checked').length;
            if (checkedCount === checkboxes.length) {
                checkboxes.prop('checked', false);
                $('.hover-checkbox').hide();
                $('.acc-prfl').show();
                $('.multi-chckbx').hide();
            } else {
                checkboxes.prop('checked', true);
                $('.acc-prfl').hide();
            }

            updateTableBackground();
        });

        $('.hover-checkbox').change(function () {
            updateTableBackground();
        });

        //responsive view
        mbNewMessage = document.getElementById('mb-new-message');
        const allMessagesItems = document.querySelectorAll('.secured-messenger .all-msg .hover-table');
        let topDesign = document.querySelector('.top-design');
        topDesign.style.display = 'none';

        function onClickNewMessagebtn() {
            $("#message-id-new-message-hidden").val("@Guid.Empty.ToString()");
            $("#new-message-form").submit();
        }

        mbNewMessage.addEventListener('click', onClickNewMessagebtn);

        allMessagesItems.forEach(item => {
            item.addEventListener('click', function () {
                hideAllMessages();
                if (window.innerWidth <= 979) {
                    topDesign.style.display = 'block';
                    topDesign.classList.add('active');
                }
            });
        });

        $('#create-new-message').click(function () {
            $('.view-section').hide();
        });

        window.initSearchEvents();
    });
};

//delete draft
window.onDeleteDraftbtnClick = function (messageId) {
    $("#secured-mess-body").val("");
    $("#new-message-section").hide();

    if ($(".view-section").length > 1) {
        $("#reply-button-" + messageId).show();
        return;
    }

    $(".view-section").hide();
    $(".empty-sec").show();
};

//search
window.showLoadingEffect = () => document.getElementById("messenger-keyword").classList.add("loading");

window.hideLoadingEffect = () => setTimeout(() =>
    document.getElementById("messenger-keyword").classList.remove("loading"), 300);

window.initSearchEvents = () => {
    let input = document.getElementById("messenger-keyword"), form = document.getElementById("search-msg-form");
    input.addEventListener("keyup", () => {
        showLoadingEffect();
        clearTimeout(window.searchTimeout);
        window.searchTimeout = setTimeout(() => form.submit(), 300);
    });
    form.addEventListener("submit", showLoadingEffect);
};

window.preventFormSubmit = () => {
    document.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            const activeEl = document.activeElement;
            if (activeEl?.tagName === "INPUT" && activeEl.type === "text") {
                e.preventDefault();
            }
        }
    });
};

const addedEmails = Array.from(document.querySelectorAll('.email-text'))
    .map(el => el.textContent.trim().toLowerCase());

window.attachValidationOnKey = function (elementId) {
    const input = document.getElementById(elementId);

    if (input) {
        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === "," || event.code === "Comma" || event.keyCode === 188) {
                let value = input.value.trim().toLowerCase();

                if (value.endsWith(",")) {
                    value = value.slice(0, -1).trim();
                }

                const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

                if (!emailRegex.test(value)) {
                    window.showValidationError(elementId, "Please enter a valid email address.");
                } else if (addedEmails.includes(value)) {
                    window.showValidationError(elementId, "This email is already added.");
                } else {
                    addedEmails.push(value);
                    input.value = "";
                }
            }
        });
    }
};

window.showValidationError = function (elementId, message) {
    const input = document.getElementById(elementId);
    if (input) {
        input.setCustomValidity(message);
        input.reportValidity();

        setTimeout(() => {
            input.setCustomValidity("");
            input.reportValidity();
        }, 1000);
    }
};