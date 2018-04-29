import * as Constants from "./constants";
import { Collection } from "./collection";
import { InputWithPopup, PopupControl } from "./inputwithpopup";

export class DropDownItem {
    private _element: HTMLElement;
    private _value: string;

    private click() {
        if (this.onClick) {
            this.onClick(this);
        }
    }

    readonly key: string;

    onClick: (item: DropDownItem) => void;

    constructor(key: string, value: string) {
        this.key = key;
        this._value = value;
    }

    toString(): string {
        return this.value;
    }

    render(): HTMLElement {
        if (!this._element) {
            this._element = document.createElement("span");
            this._element.className = "ms-ctrl ms-ctrl-dropdown-item";
            this._element.innerText = this.value;
            this._element.onclick = (e) => { this.click(); }
        }

        return this._element;
    }

    get value(): string {
        return this._value;
    }

    set value(newValue: string) {
        this._value = newValue;

        if (this._element) {
            this._element.innerText = newValue;
        }
    }
}

export class DropDownPopupControl extends PopupControl {
    private _owner: DropDown;
    private _renderedItems: Array<HTMLElement> = [];
    private _selectedIndex: number = -1;

    constructor(owner: DropDown) {
        super();

        this._owner = owner;
    }

    protected renderContent(): HTMLElement {
        var element = document.createElement("div");
        element.className = "ms-ctrl ms-popup";

        var selectedIndex = this._owner.selectedIndex;

        for (var i = 0; i < this._owner.items.length; i++) {
            var renderedItem = this._owner.items.get(i).render();
            renderedItem.tabIndex = 0;

            element.appendChild(renderedItem);

            if (i == selectedIndex) {
                renderedItem.focus();
            }

            this._renderedItems.push(renderedItem);
        }

        return element;
    }

    keyDown(e: KeyboardEvent) {
        super.keyDown(e);

        var selectedItemIndex = this._selectedIndex;

        switch (e.keyCode) {
            case Constants.KEY_TAB:
                this.close();

                break;
            case Constants.KEY_ENTER:
                if (this.selectedIndex >= 0) {
                    this._owner.selectedIndex = this.selectedIndex;

                    this.close();
                }

                break;
            case Constants.KEY_UP:
                if (selectedItemIndex <= 0) {
                    selectedItemIndex = this._renderedItems.length - 1;
                }
                else {
                    selectedItemIndex--;

                    if (selectedItemIndex < 0) {
                        selectedItemIndex = this._renderedItems.length - 1;
                    }
                }

                this.selectedIndex = selectedItemIndex;

                e.cancelBubble = true;
                
                break;
            case Constants.KEY_DOWN:
                if (selectedItemIndex < 0) {
                    selectedItemIndex = 0;
                }
                else {
                    selectedItemIndex++;

                    if (selectedItemIndex >= this._renderedItems.length) {
                        selectedItemIndex = 0;
                    }
                }

                this.selectedIndex = selectedItemIndex;

                e.cancelBubble = true;
                
                break;
        }
    }

    render(rootElementBounds: ClientRect) {
        var renderedElement = super.render(rootElementBounds);

        renderedElement.style.minWidth = (rootElementBounds.width / 2) + "px";
        renderedElement.style.maxWidth = rootElementBounds.width + "px";                
        
        return renderedElement;
    }

    get selectedIndex(): number {
        return this._selectedIndex;
    }

    set selectedIndex(index: number) {
        if (index >= 0 && index < this._renderedItems.length) {
            this._renderedItems[index].focus();

            this._selectedIndex = index;
        }
    }
}

export class DropDown extends InputWithPopup<DropDownPopupControl, DropDownItem> {
    private _items: Collection<DropDownItem>;

    private itemClicked(item: DropDownItem) {
        this.selectedItem = item;
        this.closePopup();

        this.rootElement.focus();
    }

    protected validateRootElement(rootElement: HTMLElement) {
        if (!(rootElement instanceof HTMLDivElement)) {
            throw new Error("DropDown requires a DIV element as its root.");
        }
    }

    protected createPopupControl(): DropDownPopupControl {
        return new DropDownPopupControl(this);
    }

    protected getCssClassName(): string {
        return "ms-ctrl ms-ctrl-dropdown";
    }

    constructor() {
        super();
        
        this._items = new Collection<DropDownItem>();
        this._items.onItemAdded = (item) => { item.onClick = (clickedItem) => { this.itemClicked(clickedItem); }; }
        this._items.onItemRemoved = (item) => { item.onClick = null; }
    }

    attach(rootElement: HTMLElement) {
        super.attach(rootElement);

        for (var i = 0; i < this.rootElement.children.length; i++) {
            var childElement = this.rootElement.children[i];

            if (childElement.tagName.toLowerCase() == "ms-dropdown-item") {
                var item = new DropDownItem(
                    childElement.attributes.getNamedItem("key").value,
                    childElement.attributes.getNamedItem("value").value);

                this._items.add(item);
            }
        }
    }

    popup() {
        super.popup();

        this.popupControl.selectedIndex = this.selectedIndex;
    }

    get items(): Collection<DropDownItem> {
        return this._items;
    }

    get selectedItem(): DropDownItem {
        return <DropDownItem>this.value;
    }

    set selectedItem(newValue: DropDownItem) {
        this.value = newValue;
    }

    get selectedIndex(): number {
        return this.items.indexOf(this.value);
    }

    set selectedIndex(index: number) {
        if (index >= 0 && this.items.length > index) {
            this.selectedItem = this.items.get(index);
        }
    }
}