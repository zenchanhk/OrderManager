package ibcontroller;

import javax.swing.*;
import java.awt.*;
import java.awt.event.WindowEvent;

public class SecurityCodeFailureDialogHandler  implements WindowHandler {
    public boolean filterEvent(Window window, int eventId) {
        switch (eventId) {
            case WindowEvent.WINDOW_OPENED:
                return true;
            default:
                return false;
        }
    }

    public void handleWindow(Window window, int eventID) {
        if (SwingUtils.clickButton(window, "OK")) {
        } else {
            Utils.logError("could not handle security code failure confirmation dialog because we could not find one of the controls.");
        }
    }

    public boolean recogniseWindow(Window window) {
        if (! (window instanceof JDialog)) return false;
        Utils.logToFile(SwingUtils.getWindowStructure(window));
        return (SwingUtils.findLabel(window, "incorrect security code") != null);
    }
}
