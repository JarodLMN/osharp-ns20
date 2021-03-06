import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations'; // this is needed!
import { NgModule } from '@angular/core';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { TranslateService, TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';

import { AppRoutingModule } from "./app.routing";
import { AppComponent } from './app.component';

import { AngleCoreModule } from "./shared/angle/core/angle.core.module";
import { AngleModule } from './shared/angle/angle.module';
import { SharedModule } from "./shared/shared.module";
import { LayoutModule } from './layout/layout.module';
import { HomeModule } from "./home/home.module";
import { AdminModule } from "./admin/admin.module";

// https://github.com/ocombe/ng2-translate/issues/218
export function createTranslateLoader(http: HttpClient) {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

@NgModule({
    declarations: [
        AppComponent
    ],
    imports: [
        HttpClientModule,
        BrowserAnimationsModule, // required for ng2-tag-input
        AngleCoreModule,
        AngleModule.forRoot(),
        SharedModule,
        AppRoutingModule,
        HomeModule,
        LayoutModule,
        TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useFactory: (createTranslateLoader),
                deps: [HttpClient]
            }
        })
    ],
    providers: [],
    bootstrap: [AppComponent]
})
export class AppModule { }
