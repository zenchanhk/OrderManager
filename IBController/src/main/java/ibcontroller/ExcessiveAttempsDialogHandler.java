package ibcontroller;

import org.bytedeco.javacpp.presets.opencv_core;
import org.joda.time.DateTime;

import javax.swing.*;
import java.awt.*;
import java.awt.event.WindowEvent;

public class ExcessiveAttempsDialogHandler implements WindowHandler {
    private static int pauseDuration = 0;
    private static DateTime lastSetTime = DateTime.now();

    public static DateTime getLastSetTime() { return lastSetTime; }
    public static int getPauseDuration() { return pauseDuration; }
    public static void setPauseDuration(int val) { pauseDuration = val; }

    public boolean filterEvent(Window window, int eventId) {
        switch (eventId) {
            case WindowEvent.WINDOW_OPENED:
                return true;
            default:
                return false;
        }
    }

    public void handleWindow(Window window, int eventID) {
        String text = SwingUtils.getTextAreaContent(window, 0);
        if (SwingUtils.clickButton(window, "OK")) {
            Utils.logToFile("Login Wait Text:" + text);

            String[] content = text.split(" ");
            int min = 0;
            int sec = 0;
            for (int i = 0; i < content.length; i++) {
                if (content[i].contains("minute"))
                    min = Integer.parseInt(content[i - 1].trim());
                if (content[i].contains("second"))
                    sec = Integer.parseInt(content[i - 1].trim());
            }
            setPauseDuration(min * 60 + sec + 5);
            lastSetTime = DateTime.now();
        } else {
            Utils.logError("could not handle security code failure confirmation dialog because we could not find one of the controls.");
        }
    }

    public boolean recogniseWindow(Window window) {
        if (! (window instanceof JDialog)) return false;
        Utils.logToFile(SwingUtils.getWindowStructure(window));
        return (SwingUtils.findTextArea(window, "Too many failed login attempts"));
    }
}
