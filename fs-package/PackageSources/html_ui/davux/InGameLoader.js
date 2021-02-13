function CreateInGameLoader() {
    Include.addScript("/davux/SharedUtils.js", () => {
        Include.addScript("/davux/InGameRelay.js", () => {
            Include.addScript("/davux/InGameCommLink.js");
        });
    });
}

CreateInGameLoader();