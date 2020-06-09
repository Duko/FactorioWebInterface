import "./modal.ts.less";
import { ContentPresenter } from "./contentPresenter";
import { EventListener } from "../utils/eventListener";
import { Button } from "./button";
export class Modal extends HTMLElement {
    constructor(content) {
        super();
        this._headerPanel = document.createElement('header');
        this._contentPresenter = new ContentPresenter();
        this._footerPanel = document.createElement('footer');
        this._contentPresenter.setContent(content);
        this.addCloseButton();
        this.append(this._headerPanel, this._contentPresenter, this._footerPanel);
    }
    setHeader(value) {
        let first = this._headerPanel.firstChild;
        if (first == null || first === this._closeButton) {
            this._headerPanel.prepend(value);
            return this;
        }
        first.remove();
        this._headerPanel.prepend(value);
        return this;
    }
    setContent(value) {
        this._contentPresenter.setContent(value);
        return this;
    }
    onClose(callback) {
        return EventListener.onClick(this._closeButton, callback);
    }
    addCloseButton() {
        this._closeButton = new Button(null, Button.classes.close);
        this._closeButton.style.marginLeft = 'auto';
        this._headerPanel.append(this._closeButton);
    }
}
customElements.define('a-modal', Modal);
//# sourceMappingURL=modal.js.map