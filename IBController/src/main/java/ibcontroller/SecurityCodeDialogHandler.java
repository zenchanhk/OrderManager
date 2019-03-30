// This file is part of the "IBController".
// Copyright (C) 2004 Steven M. Kearns (skearns23@yahoo.com )
// Copyright (C) 2004 - 2015 Richard L King (rlking@aultan.com)
// For conditions of distribution and use, see copyright notice in COPYING.txt

// IBController is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// IBController is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with IBController.  If not, see <http://www.gnu.org/licenses/>.

package ibcontroller;

import java.awt.*;
import java.awt.event.WindowEvent;
import java.io.IOException;
import java.nio.charset.Charset;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.*;
import java.util.List;
import javax.swing.*;


public class SecurityCodeDialogHandler implements WindowHandler {

    private static Map<String, String> codes = new HashMap<String, String>();

    @Override
    public boolean filterEvent(Window window, int eventId) {
        switch (eventId) {
            case WindowEvent.WINDOW_OPENED:
                return true;
            default:
                return false;
        }
    }

    @Override
    public void handleWindow(Window window, int eventID) {
        // initialize codes
        if (codes.size() == 0) {
            try {
                Path p = Paths.get(Utils.getBaseDir(), "encode");
                List<String> lines = Files.readAllLines(p, Charset.defaultCharset());
                for (String line : lines) {
                    if (line != null && !line.isEmpty()) {
                        String[] kv = Encryptor.decrypt(line).split(";");
                        codes.put(kv[0], kv[1]);
                    }
                }
            } catch (Exception ex) {
                Utils.logToFile("Failed to read code");
            }
        }

        Component image = SwingUtils.findComponentByClass(window, "twslaunch.jauthentication.bg");
        if (image != null){
            String[] paths = SwingUtils.captureComponent(image);
            if (paths != null) {
                List<String> captcha = new ArrayList<>();
                List<String> decoded = new ArrayList<>();
                for (String p: paths) {
                    //System.out.println("Path: " + p);
                    String text = SwingUtils.captchaDecoder(p);
                    //System.out.println("Decode: " + text);
                    if (!text.isEmpty()) {
                        captcha.add(text);
                        decoded.add(codes.get(text));
                        //Utils.logToFile("captcha:" + text + ", code:" + codes.get(text));
                    } else {
                        // enter arbitrary character to invalid security code and re-enter login form
                        Utils.logToFile("Failed to recognize captcha images - " + p);
                        return;
                    }
                }
                /*
                int dialogResult = JOptionPane.showConfirmDialog (null,
                        "Would You Like to continue verification process?\n"
                        + "First captcha: " + decoded.get(0) + "\n"
                        + "Second captcha: " + decoded.get(1),
                        "Warning",JOptionPane.YES_NO_OPTION);*/
                //if(dialogResult == JOptionPane.YES_OPTION){
                    // Saving code here
                    SwingUtils.setTextField(window, 0,  String.join(" ", decoded));
                //} else {
                    // give up continuing
                //    SwingUtils.setTextField(window, 0, "A");
                //}
                SwingUtils.clickButton(window, "Submit");

            } else {
                Utils.logToFile("Failed to save captcha images.");
            }
        }
    }

    @Override
    public boolean recogniseWindow(Window window) {

        if (! (window instanceof JDialog)) return false;
        //return (SwingUtils.findButton(window, "Enter Read Only") != null);
        //JLabel label = SwingUtils.findLabel(window, "Card Values:");
        //if (label != null)

        boolean found = SwingUtils.titleContains(window, "Card");
        //Utils.logToFile(((JDialog) window).getTitle());
        if (found)
            Utils.logToFile("found");
        //Utils.logToFile(SwingUtils.getWindowStructure(window));

        //Utils.logToConsole(SwingUtils.getWindowStructure(window));
        return found;
    }


}
