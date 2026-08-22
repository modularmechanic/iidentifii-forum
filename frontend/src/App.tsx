import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Link, Route, Routes } from 'react-router';
import AppShell from '@src/components/common/ui/lg/AppShell';
import Home from '@src/components/pages/Home/Home';
import EditDiscussion from '@src/components/pages/Discussions/Edit/EditDiscussion';
import NewDiscussion from '@src/components/pages/Discussions/New/NewDiscussion';
import ViewDiscussion from '@src/components/pages/Discussions/View/ViewDiscussion';
import CheckInbox from '@src/components/pages/Auth/CheckInbox/CheckInbox';
import ForgotPassword from '@src/components/pages/Auth/ForgotPassword/ForgotPassword';
import Login from '@src/components/pages/Auth/Login/Login';
import LoginCode from '@src/components/pages/Auth/LoginCode/LoginCode';
import Register from '@src/components/pages/Auth/Register/Register';
import ResetPassword from '@src/components/pages/Auth/ResetPassword/ResetPassword';
import VerifyEmail from '@src/components/pages/Auth/VerifyEmail/VerifyEmail';
import Paths from '@src/domains/common/constants/Paths';
import AuthProvider from '@src/infra/auth/AuthProvider';
import RequireAuth from '@src/components/common/RequireAuth';

/***** Constants *****/

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
});

/***** Components *****/

/** Default component: wires routing and server-state caching around the shell. */
function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <AppShell>
            <Routes>
              <Route element={<Home />} path={Paths.Home} />
              <Route
                element={
                  <RequireAuth>
                    <NewDiscussion />
                  </RequireAuth>
                }
                path={Paths.NewDiscussion}
              />
              <Route
                element={
                  <RequireAuth>
                    <EditDiscussion />
                  </RequireAuth>
                }
                path={Paths.EditDiscussionPattern}
              />
              <Route element={<ViewDiscussion />} path={Paths.DiscussionPattern} />
              <Route element={<Register />} path={Paths.Register} />
              <Route element={<CheckInbox />} path={Paths.CheckInbox} />
              <Route element={<VerifyEmail />} path={Paths.VerifyEmail} />
              <Route element={<Login />} path={Paths.Login} />
              <Route element={<LoginCode />} path={Paths.LoginCode} />
              <Route element={<ForgotPassword />} path={Paths.ForgotPassword} />
              <Route element={<ResetPassword />} path={Paths.ResetPassword} />
              <Route element={<NotFound />} path="*" />
            </Routes>
          </AppShell>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

/** Shown when no route matches the address. */
function NotFound() {
  return (
    <section className="py-12 text-center">
      <h1 className="text-lg font-semibold">Page not found</h1>
      <p className="mt-1 text-sm text-muted">
        Check the address, or{' '}
        <Link className="text-accent underline" to={Paths.Home}>
          back to all discussions
        </Link>
        .
      </p>
    </section>
  );
}

/***** Export default *****/

export default App;
